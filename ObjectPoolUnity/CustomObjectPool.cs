/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：CustomObjectPool
 * 
 * 创建者：Frandys
 * 修改者：Claude AI
 * 创建时间：3/6/2025 5:34:47 PM
 * 描述：优化的对象池实现，支持自动扩展、性能监控和线程安全
 *----------------------------------------------------------------*/


using System;
using System.Collections.Generic;
using UnityEngine;
using System.Collections.Concurrent;
using UnityGameFramework.Runtime;

namespace BEWGame.Pool
{
	/// <summary>
	/// 优化的对象池实现
	/// 支持自动扩展、性能监控和线程安全
	/// </summary>
	/// <typeparam name="T">池对象组件类型</typeparam>
	internal class CustomObjectPool<T> : IPoolManager where T : Component, IPoolObject
	{
		// 非活跃对象栈，用于快速获取对象
		private ConcurrentStack<T> _inactiveObjects = new ConcurrentStack<T>();
		// 活跃对象集合，用于跟踪和回收
		private ConcurrentDictionary<T, byte> _activeObjects = new ConcurrentDictionary<T, byte>();
		// 对象生成时间字典，用于计算生命周期
		private ConcurrentDictionary<T, float> _spawnTimes = new ConcurrentDictionary<T, float>();

		// 对象池根节点
		private Transform _poolRoot;

		// 预制体
		private GameObject _prefab;

		// 最大对象数量
		private int _maxCount;

		// 初始对象数量
		private int _initialCount;

		// 线程锁对象
		private readonly object _lockObject = new object();

		// 是否已初始化
		private bool _isInitialized = false;

		// 是否已销毁
		private bool _isDisposed = false;

		// 自动清理计时器
		private float _lastCleanupTime = 0f;

		// 自动清理间隔（秒）
		private const float CLEANUP_INTERVAL = 60f;

		// 对象过期时间（秒）
		private const float OBJECT_EXPIRY_TIME = 300f;

		// 最小保留对象数量
		private int _minRetainCount;

		// 对象池配置
		private PoolPrefabConfig _config;

		// 对象池状态
		private bool _isPaused = false;

		// 对象池类型
		private string _poolType;

		// 添加自动扩展相关字段
		private bool _enableAutoExpand = true;
		private float _autoExpandRatio = 0.2f;

		// 热池相关变量 - 使用线程安全的集合
		private ConcurrentQueue<IPoolObject> _hotPool = new ConcurrentQueue<IPoolObject>();
		private int _hotPoolCapacity = 10; // 热池默认容量
		private ConcurrentDictionary<IPoolObject, float> _objectUsageFrequency = new ConcurrentDictionary<IPoolObject, float>();
		private float _hotPoolThreshold = 0.7f; // 使用频率阈值，超过此值的对象会进入热池

		// 添加性能优化相关字段
		private const int USAGE_UPDATE_BATCH_SIZE = 20; // 批量更新大小
		private const float USAGE_UPDATE_INTERVAL = 5.0f; // 使用频率更新间隔（秒）
		private float _lastUsageUpdateTime = 0f; // 上次使用频率更新时间
		private Queue<IPoolObject> _pendingUsageUpdates = new Queue<IPoolObject>(); // 待更新使用频率的对象队列
		private bool _isOptimizingHotPool = false; // 是否正在优化热池
		private float _lastHotPoolOptimizeTime = 0f; // 上次热池优化时间
		private const float HOT_POOL_OPTIMIZE_INTERVAL = 30.0f; // 热池优化间隔（秒）
		public Transform PoolRoot => _poolRoot;

		/// <summary>
		/// 当前活跃对象数量
		/// </summary>
		public int ActiveCount => _activeObjects.Count;

		/// <summary>
		/// 对象池总容量
		/// </summary>
		public int TotalCapacity => _inactiveObjects.Count + _activeObjects.Count;

		/// <summary>
		/// 对象池可用对象数量
		/// </summary>
		public int AvailableCount => _inactiveObjects.Count;

		/// <summary>
		/// 获取非活跃对象数量
		/// </summary>
		/// <returns>非活跃对象数量</returns>
		public int GetInactiveCount()
		{
			return _inactiveObjects.Count;
		}

		/// <summary>
		/// 对象池最大容量
		/// </summary>
		public int MaxCapacity => _maxCount;

		/// <summary>
		/// 对象池类型
		/// </summary>
		public string PoolType => _poolType;

		/// <summary>
		/// 获取对象池类型
		/// </summary>
		/// <returns>对象池类型</returns>
		public string GetPoolType()
		{
			return _poolType;
		}

		/// <summary>
		/// 对象池是否已初始化
		/// </summary>
		public bool IsInitialized => _isInitialized;

		/// <summary>
		/// 对象池是否已销毁
		/// </summary>
		public bool IsDisposed => _isDisposed;

		/// <summary>
		/// 对象池ID
		/// </summary>
		public string PoolId => _poolType;

		/// <summary>
		/// 预制体
		/// </summary>
		public GameObject Prefab => _prefab;

		/// <summary>
		/// 是否启用自动扩展
		/// </summary>
		public bool EnableAutoExpand => _enableAutoExpand;

		/// <summary>
		/// 自动扩展比例
		/// </summary>
		public float AutoExpandRatio => _autoExpandRatio;

		/// <summary>
		/// 对象池配置
		/// </summary>
		public PoolPrefabConfig Config => _config;


		/// <summary>
		/// 初始化对象池
		/// </summary>
		/// <param name="prefab">预制体</param>
		/// <param name="initialCount">初始数量</param>
		/// <param name="maxCount">最大数量</param>
		public void Init(string tp, GameObject prefab, int initialCount, int maxCount)
		{
			if (_isInitialized)
			{
				PoolLogger.Warning($"[OptimizedObjectPool] 对象池 {typeof(T).Name} 已初始化，忽略重复初始化");
				return;
			}

			if (prefab == null)
			{
				throw new ArgumentNullException(nameof(prefab), "预制体不能为空");
			}

			_prefab = prefab;
			_initialCount = Mathf.Max(0, initialCount);
			_maxCount = Mathf.Max(_initialCount, maxCount);
			_minRetainCount = Mathf.Max(5, _initialCount / 2);

			// 设置池ID
			_poolType = tp;

			// 创建对象池配置
			_config = new PoolPrefabConfig(tp, prefab, initialCount, maxCount, _minRetainCount);

			CreatePoolRoot();
			_isInitialized = true;
			Prewarm(_initialCount);

			_lastCleanupTime = Time.time;

			PoolLogger.Info($"[OptimizedObjectPool] 初始化对象池 {typeof(T).Name}，初始容量: {_initialCount}，最大容量: {_maxCount}");
		}

		/// <summary>
		/// 创建对象池根节点
		/// </summary>
		private void CreatePoolRoot()
		{
			GameObject rootObj = new GameObject($"Pool_{_poolType}");
			_poolRoot = rootObj.transform;
			_poolRoot.position = Vector3.zero;
		}

		/// <summary>
		/// 预热对象池，预先创建指定数量的对象
		/// </summary>
		/// <param name="count">预热数量</param>
		public void Prewarm(int count)
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[OptimizedObjectPool] 对象池 {_poolType} 已销毁，无法预热");
				return;
			}

			CheckInitialized();

			lock (_lockObject)
			{
				int createCount = Mathf.Min(count, _maxCount - TotalCapacity);
				if (createCount <= 0) return;

				// Batch creation
				List<T> newObjects = new List<T>();
				for (int i = 0; i < createCount; i++)
				{
					T obj = CreateNewObject();
					if (obj != null)
					{
						newObjects.Add(obj);
					}
				}

				// Add to inactive stack in batch
				foreach (var obj in newObjects)
				{
					_inactiveObjects.Push(obj);
				}

				PoolLogger.Info($"[OptimizedObjectPool] 预热对象池 {_poolType}，创建 {createCount} 个对象");
			}
		}

		/// <summary>
		/// 创建新对象
		/// </summary>
		/// <returns>新创建的对象</returns>
		private T CreateNewObject()
		{
			if (_prefab == null)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 创建对象失败，预制体为空");
				return null;
			}

			try
			{
				// 实例化对象
				GameObject obj = GameObject.Instantiate(_prefab, _poolRoot);
				obj.name = $"{_prefab.name}_{TotalCapacity}";

				// 获取组件
				T component = obj.GetComponent<T>();
				if (component == null)
				{
					PoolLogger.Error($"[CustomObjectPool] 预制体 {_prefab.name} 缺少组件 {typeof(T).Name}");
					GameObject.Destroy(obj);
					return null;
				}

				// 初始化对象
				component.OnCreate(_poolType);
				component.gameObject.SetActive(false);

				return component;
			}
			catch (Exception e)
			{
				PoolLogger.Error($"[CustomObjectPool] 创建对象失败: {e.Message}");
				return null;
			}
		}

		/// <summary>
		/// 检查是否已初始化
		/// </summary>
		private void CheckInitialized()
		{
			if (!_isInitialized)
			{
				throw new InvalidOperationException($"对象池 {_poolType} 未初始化");
			}
		}

		/// <summary>
		/// 获取对象
		/// </summary>
		/// <returns>池对象</returns>
		public IPoolObject Get()
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法获取对象");
				return null;
			}

			CheckInitialized();

			if (_isPaused)
			{
				PoolLogger.Warning($"[CustomObjectPool] 对象池 {_poolType} 已暂停，无法获取对象");
				return null;
			}

			float startTime = Time.realtimeSinceStartup;

			// 使用锁确保线程安全
			lock (_lockObject)
			{
				T obj = null;

				// 首先尝试从热池获取对象
				if (_hotPool.Count > 0)
				{
					IPoolObject hotPoolObj;
					if (_hotPool.TryDequeue(out hotPoolObj))
					{
						T hotObj = hotPoolObj as T;
						if (hotObj != null && hotObj.gameObject != null)
						{
							// 记录获取时间
							float getTime = (Time.realtimeSinceStartup - startTime) * 1000;

							// 添加到活跃对象字典
							_activeObjects[hotObj] = 0;
							_spawnTimes[hotObj] = Time.time;

							// 激活对象
							hotObj.gameObject.SetActive(true);
							hotObj.OnSpawn();

							// 更新时间
							hotObj.LastUsedTime = Time.time;
							hotObj.LastActiveTime = Time.time;

							// 更新使用频率
							UpdateObjectUsageFrequency(hotObj);

							return hotObj;
						}
					}
				}

				// 热池没有可用对象，从主池获取
				// 尝试从非活跃栈获取对象
				if (!_inactiveObjects.TryPop(out obj))
				{
					// 如果栈为空，检查是否可以创建新对象
					if (TotalCapacity < _maxCount)
					{
						obj = CreateNewObject();
						if (obj == null)
						{
							PoolLogger.Error($"[CustomObjectPool] 创建对象失败");
							return null;
						}

					}
					else
					{
						// 池已满，检查是否可以自动扩展
						if (_config != null && _config.allowAutoExpand && _enableAutoExpand)
						{
							// 自动扩展池容量
							int expandAmount = Mathf.Max(1, Mathf.FloorToInt(_maxCount * _autoExpandRatio));
							ExpandPool(expandAmount);

							if (!_inactiveObjects.TryPop(out obj))
							{
								// 扩展后仍无法获取对象
								PoolLogger.Warning($"[CustomObjectPool] 对象池 {_poolType} 扩展后仍无法获取对象");
								return null;
							}
						}
						else
						{
							// 池已满，记录并返回null
							PoolLogger.Warning($"[CustomObjectPool] 对象池 {_poolType} 已达到最大容量 {_maxCount}，无法获取对象");
							return null;
						}
					}
				}

				// 记录获取时间
				float finalGetTime = (Time.realtimeSinceStartup - startTime) * 1000;

				// 添加到活跃集合
				_activeObjects[obj] = 0;

				// 记录生成时间
				_spawnTimes[obj] = Time.time;

				// 激活对象
				obj.gameObject.SetActive(true);
				obj.OnSpawn();
				obj.LastActiveTime = Time.time;
				obj.LastUsedTime = Time.time;

				// 更新使用频率
				UpdateObjectUsageFrequency(obj);

				return obj;
			}
		}

		/// <summary>
		/// 获取对象（泛型版本）
		/// </summary>
		/// <typeparam name="TObj">对象类型</typeparam>
		/// <returns>池对象</returns>
		public TObj Get<TObj>() where TObj : Component, IPoolObject
		{
			return Get() as TObj;
		}

		/// <summary>
		/// 回收对象
		/// </summary>
		/// <param name="obj">要回收的对象</param>
		public void Release(IPoolObject obj)
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法回收对象");
				return;
			}

			if (obj == null)
			{
				PoolLogger.Warning($"[OptimizedObjectPool] 回收对象失败：对象为空");
				return;
			}

			if (obj is T typedObj)
			{
				// 检查对象是否属于此池
				if (obj.PoolType != _poolType)
				{
					PoolLogger.Error($"[CustomObjectPool] 对象 {obj.gameObject.name} 不属于此池 {_poolType}，而是 {obj.PoolType}");
					return;
				}

				// 使用锁确保线程安全
				lock (_lockObject)
				{
					// 检查对象是否已在活跃集合中
					byte dummy;
					if (!_activeObjects.TryRemove(typedObj, out dummy))
					{
						// 对象可能已经被回收
						PoolLogger.Warning($"[CustomObjectPool] 对象 {obj.gameObject.name} 不在活跃集合中，可能已被回收");
						return;
					}

					// 记录对象生命周期
					float spawnTime;
					if (_spawnTimes.TryRemove(typedObj, out spawnTime))
					{
						float lifetime = Time.time - spawnTime;

					}

					// 调用对象的回收方法
					obj.OnDespawn();

					// 禁用对象
					obj.gameObject.SetActive(false);

					// 重置对象变换
					typedObj.transform.SetParent(_poolRoot);
					typedObj.transform.localPosition = Vector3.zero;
					typedObj.transform.localRotation = Quaternion.identity;
					typedObj.transform.localScale = Vector3.one;

					// 检查对象使用频率，决定放入热池还是主池
					float frequency;
					if (_objectUsageFrequency.TryGetValue(obj, out frequency) && frequency > _hotPoolThreshold && _hotPool.Count < _hotPoolCapacity)
					{
						_hotPool.Enqueue(typedObj);
					}
					else
					{
						// 添加到非活跃栈
						_inactiveObjects.Push(typedObj);
					}
				}
			}
			else
			{
				PoolLogger.Error($"[OptimizedObjectPool] 回收对象失败：对象类型不匹配，期望 {typeof(T).Name}，实际 {obj.GetType().Name}");
			}
		}

		/// <summary>
		/// 回收所有对象
		/// </summary>
		public void ReleaseAll()
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法回收对象");
				return;
			}

			// 复制活跃对象集合，避免在迭代过程中修改集合
			var activeObjects = new List<T>(_activeObjects.Keys);

			foreach (var obj in activeObjects)
			{
				if (obj != null)
				{
					Release(obj);
				}
			}

			PoolLogger.Info($"[CustomObjectPool] 回收对象池 {_poolType} 的所有对象，共 {activeObjects.Count} 个");
		}

		/// <summary>
		/// 清理过期对象
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		public int CleanupExpiredObjects(int count = 0)
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法清理对象");
				return 0;
			}

			// 如果没有指定数量，计算一个合理的清理数量
			if (count <= 0)
			{
				// 保留最小数量的对象
				int availableForCleanup = Mathf.Max(0, _inactiveObjects.Count - _minRetainCount);

				// 默认清理一半可清理的对象
				count = availableForCleanup / 2;
			}

			// 如果没有可清理的对象，直接返回
			if (count <= 0 || _inactiveObjects.Count <= _minRetainCount)
			{
				return 0;
			}

			return CleanupExcessObjects(count);
		}

		/// <summary>
		/// 清理非活跃对象
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		public int CleanupInactiveObjects(int count = 0)
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法清理非活跃对象");
				return 0;
			}

			// 如果没有指定数量，计算一个合理的清理数量
			if (count <= 0)
			{
				// 保留最小数量的对象
				int availableForCleanup = Mathf.Max(0, _inactiveObjects.Count - _minRetainCount);

				// 默认清理一半可清理的对象
				count = availableForCleanup / 2;
			}

			// 如果没有可清理的对象，直接返回
			if (count <= 0 || _inactiveObjects.Count <= _minRetainCount)
			{
				return 0;
			}

			return CleanupExcessObjects(count);
		}

		/// <summary>
		/// 清理多余对象
		/// </summary>
		/// <param name="count">清理数量</param>
		/// <returns>实际清理的数量</returns>
		private int CleanupExcessObjects(int count)
		{
			int cleanedCount = 0;

			for (int i = 0; i < count; i++)
			{
				if (_inactiveObjects.TryPop(out T obj))
				{
					if (obj != null)
					{
						try
						{
							GameObject.Destroy(obj.gameObject);
							cleanedCount++;
						}
						catch (Exception e)
						{
							PoolLogger.Error($"[CustomObjectPool] 销毁对象失败: {e.Message}");
						}
					}
				}
				else
				{
					// 栈为空，停止清理
					break;
				}
			}

			if (cleanedCount > 0)
			{
				PoolLogger.Info($"[CustomObjectPool] 清理对象池 {_poolType} 的 {cleanedCount} 个对象");
			}

			return cleanedCount;
		}

		/// <summary>
		/// 设置对象池配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		public void SetConfig(PoolPrefabConfig config)
		{
			if (config == null)
			{
				PoolLogger.Warning($"[OptimizedObjectPool] 设置配置失败：配置为空");
				return;
			}

			_config = config;
		}

		/// <summary>
		/// 获取对象池配置
		/// </summary>
		/// <returns>对象池配置</returns>
		public PoolPrefabConfig GetConfig()
		{
			return _config;
		}

		/// <summary>
		/// 重置对象池
		/// </summary>
		public void Reset()
		{
			if (_isDisposed)
			{
				PoolLogger.Error($"[CustomObjectPool] 对象池 {_poolType} 已销毁，无法重置");
				return;
			}

			// 回收所有对象
			ReleaseAll();

			// 清理所有对象
			CleanupExpiredObjects(_inactiveObjects.Count);

			// 重新预热
			Prewarm(_initialCount);

			PoolLogger.Info($"[CustomObjectPool] 重置对象池 {_poolType}");
		}

		/// <summary>
		/// 暂停对象池
		/// </summary>
		public void Pause()
		{
			_isPaused = true;
			PoolLogger.Info($"[CustomObjectPool] 暂停对象池 {_poolType}");
		}

		/// <summary>
		/// 恢复对象池
		/// </summary>
		public void Resume()
		{
			_isPaused = false;
			PoolLogger.Info($"[CustomObjectPool] 恢复对象池 {_poolType}");
		}

		/// <summary>
		/// 销毁对象池
		/// </summary>
		public void Dispose()
		{
			if (_isDisposed)
			{
				return;
			}

			// 使用锁确保线程安全
			lock (_lockObject)
			{
				try
				{
					// 标记为已销毁，防止其他线程访问
					_isDisposed = true;

					// 回收所有对象
					ReleaseAll();

					// 清理所有对象并确保资源释放
					CleanupAllObjects();

					// 清理热池
					ClearHotPool();

					// 清理所有集合
					_inactiveObjects.Clear();
					_activeObjects.Clear();
					_spawnTimes.Clear();
					_objectUsageFrequency.Clear();
					_pendingUsageUpdates.Clear();

					// 销毁池根节点
					if (_poolRoot != null)
					{
						GameObject.Destroy(_poolRoot.gameObject);
						_poolRoot = null;
					}

					// 移除事件监听
					UnregisterEvents();

					// 清空配置引用
					_config = null;
					_prefab = null;

					PoolLogger.Info($"[CustomObjectPool] 销毁对象池 {_poolType} 完成");
				}
				catch (Exception e)
				{
					PoolLogger.Error($"[CustomObjectPool] 销毁对象池 {_poolType} 时出错: {e.Message}\n{e.StackTrace}");
				}
			}
		}

		/// <summary>
		/// 清理所有对象并确保资源释放
		/// </summary>
		private void CleanupAllObjects()
		{
			// 使用锁确保线程安全
			lock (_lockObject)
			{
				// 清理非活跃对象
				int cleanedCount = 0;
				T obj;

				// 创建临时列表存储所有非活跃对象
				List<T> inactiveObjectsList = new List<T>();
				while (_inactiveObjects.TryPop(out obj))
				{
					if (obj != null)
					{
						inactiveObjectsList.Add(obj);
					}
				}

				// 清理非活跃对象
				foreach (var inactiveObj in inactiveObjectsList)
				{
					try
					{
						// 调用对象的销毁方法，确保资源释放
						inactiveObj.OnDestroy();

						// 销毁游戏对象
						GameObject.Destroy(inactiveObj.gameObject);
						cleanedCount++;
					}
					catch (Exception e)
					{
						PoolLogger.Error($"[CustomObjectPool] 销毁对象失败: {e.Message}");
					}
				}

				// 创建活跃对象的快照
				List<T> activeObjectsList = new List<T>(_activeObjects.Keys);

				// 清理活跃对象（理论上应该已经通过 ReleaseAll 回收）
				foreach (var activeObj in activeObjectsList)
				{
					if (activeObj != null)
					{
						try
						{
							// 调用对象的销毁方法，确保资源释放
							activeObj.OnDestroy();

							// 销毁游戏对象
							GameObject.Destroy(activeObj.gameObject);
							cleanedCount++;

							// 从活跃对象集合中移除
							byte dummy;
							_activeObjects.TryRemove(activeObj, out dummy);
						}
						catch (Exception e)
						{
							PoolLogger.Error($"[CustomObjectPool] 销毁活跃对象失败: {e.Message}");
						}
					}
				}

				PoolLogger.Info($"[CustomObjectPool] 清理对象池 {_poolType} 的所有对象，共 {cleanedCount} 个");
			}
		}

		/// <summary>
		/// 清理热池
		/// </summary>
		private void ClearHotPool()
		{
			// 使用锁确保线程安全
			lock (_lockObject)
			{
				// 创建热池对象的快照
				List<IPoolObject> hotPoolList = new List<IPoolObject>();
				IPoolObject hotObj;
				while (_hotPool.TryDequeue(out hotObj))
				{
					if (hotObj != null)
					{
						hotPoolList.Add(hotObj);
					}
				}

				// 清理热池对象
				foreach (var obj in hotPoolList)
				{
					try
					{
						// 调用对象的销毁方法，确保资源释放
						obj.OnDestroy();

						// 销毁游戏对象
						GameObject.Destroy(obj.gameObject);
					}
					catch (Exception e)
					{
						PoolLogger.Error($"[CustomObjectPool] 销毁热池对象失败: {e.Message}");
					}
				}
			}
		}

		/// <summary>
		/// 注销事件监听
		/// </summary>
		private void UnregisterEvents()
		{
			// 内存清理请求处理
		}

		/// <summary>
		/// 内存清理请求处理
		/// </summary>
		private void OnMemoryCleanupRequested(EnumMemoryPressureLevel level, float targetReduction)
		{
			// 根据内存压力级别决定清理策略
			switch (level)
			{
				case EnumMemoryPressureLevel.Low:
					// 低压力，只清理过期对象
					CleanupExpiredObjects();
					break;
				case EnumMemoryPressureLevel.Medium:
					// 中等压力，清理一部分对象
					CleanupExpiredObjects(_inactiveObjects.Count / 4);
					break;
				case EnumMemoryPressureLevel.High:
					// 高压力，清理一半对象
					CleanupExpiredObjects(_inactiveObjects.Count / 2);
					break;
				case EnumMemoryPressureLevel.Critical:
					// 严重压力，清理大部分对象，只保留最小数量
					CleanupExpiredObjects(_inactiveObjects.Count - _minRetainCount);
					break;
			}
		}

		/// <summary>
		/// 检查不健康的对象
		/// </summary>
		private void CheckUnhealthyObjects()
		{
			// 检查活跃对象
			List<T> unhealthyObjects = new List<T>();

			foreach (var obj in _activeObjects.Keys)
			{
				if (obj == null || obj.gameObject == null)
				{
					// 对象已被销毁，添加到不健康列表
					unhealthyObjects.Add(obj);
					continue;
				}

				// 检查对象是否长时间未使用（可能泄漏）
				if (Time.time - obj.LastUsedTime > OBJECT_EXPIRY_TIME * 2)
				{
					PoolLogger.Warning($"[CustomObjectPool] 检测到可能的对象泄漏: {obj.gameObject.name}，已 {Time.time - obj.LastUsedTime} 秒未使用");
					// 可以选择强制回收或标记为不健康
					unhealthyObjects.Add(obj);
				}
			}

			// 移除不健康对象
			foreach (var obj in unhealthyObjects)
			{
				byte dummy;
				_activeObjects.TryRemove(obj, out dummy);

				// 尝试销毁对象
				try
				{
					if (obj != null && obj.gameObject != null)
					{
						obj.OnDestroy();
						GameObject.Destroy(obj.gameObject);
					}
				}
				catch (Exception e)
				{
					PoolLogger.Error($"[CustomObjectPool] 销毁不健康对象失败: {e.Message}");
				}
			}

			if (unhealthyObjects.Count > 0)
			{
				PoolLogger.Info($"[CustomObjectPool] 清理了 {unhealthyObjects.Count} 个不健康的对象");
			}
		}

		/// <summary>
		/// 更新对象池配置
		/// </summary>
		/// <param name="newConfig">新配置</param>
		public void UpdateConfig(PoolPrefabConfig newConfig)
		{
			if (newConfig == null)
			{
				PoolLogger.Warning($"[OptimizedObjectPool] 更新配置失败：配置为空");
				return;
			}

			_config = newConfig;
			_maxCount = newConfig.maxCount;
			_minRetainCount = newConfig.minRetainCount;
		}

		/// <summary>
		/// 序列化池配置
		/// </summary>
		/// <returns>序列化的配置数据</returns>
		public string SerializeConfig()
		{
			// Simple serialization for demonstration
			return JsonUtility.ToJson(_config);
		}

		/// <summary>
		/// 从序列化数据恢复配置
		/// </summary>
		/// <param name="configJson">序列化的配置数据</param>
		public void DeserializeConfig(string configJson)
		{
			if (string.IsNullOrEmpty(configJson))
			{
				PoolLogger.Warning($"[OptimizedObjectPool] 反序列化配置失败：数据为空");
				return;
			}

			try
			{
				var config = JsonUtility.FromJson<PoolPrefabConfig>(configJson);
				SetConfig(config);
			}
			catch (Exception ex)
			{
				PoolLogger.Error($"[OptimizedObjectPool] 反序列化配置失败：{ex.Message}");
			}
		}

		/// <summary>
		/// 设置自动扩展
		/// </summary>
		/// <param name="enable">是否启用自动扩展</param>
		public void SetAutoExpand(bool enable)
		{
			_enableAutoExpand = enable;
		}

		/// <summary>
		/// 设置自动扩展（扩展版本）
		/// </summary>
		/// <param name="enable">是否启用自动扩展</param>
		/// <param name="ratio">扩展比例</param>
		public void SetAutoExpand(bool enable, float ratio)
		{
			_enableAutoExpand = enable;
			_autoExpandRatio = Mathf.Clamp(ratio, 0.1f, 0.5f);
		}

		/// <summary>
		/// 定期维护对象池
		/// </summary>
		/// <param name="force">是否强制执行</param>
		public void Maintain(bool force = false)
		{
			// 检查过期对象
			if (force || Time.time - _lastCleanupTime > CLEANUP_INTERVAL)
			{
				_lastCleanupTime = Time.time;
				CleanupExpiredObjects();
			}

			// 定期优化热池
			if (force || Time.time - _lastHotPoolOptimizeTime > HOT_POOL_OPTIMIZE_INTERVAL)
			{
				OptimizeHotPool();
			}

			// 处理待更新的使用频率
			if (_pendingUsageUpdates.Count > 0)
			{
				BatchUpdateObjectUsageFrequency();
			}
		}

		/// <summary>
		/// 判断是否应该收缩池大小
		/// </summary>
		/// <returns>是否应该收缩</returns>
		private bool ShouldShrinkPool()
		{
			// 如果非活跃对象过多且使用率低，考虑收缩
			float usageRate = (float)ActiveCount / TotalCapacity;
			return _inactiveObjects.Count > _maxCount / 2 && usageRate < 0.3f;
		}

		/// <summary>
		/// 收缩池大小
		/// </summary>
		private void ShrinkPool()
		{
			int shrinkCount = _inactiveObjects.Count - _minRetainCount;
			if (shrinkCount <= 0) return;

			// 限制一次收缩的数量
			shrinkCount = Mathf.Min(shrinkCount, _inactiveObjects.Count / 4);

			CleanupExcessObjects(shrinkCount);
			PoolLogger.Info($"[OptimizedObjectPool] 收缩对象池，销毁 {shrinkCount} 个对象");
		}

		/// <summary>
		/// 判断是否应该预热池
		/// </summary>
		/// <returns>是否应该预热</returns>
		private bool ShouldPrewarmPool()
		{
			// 如果非活跃对象过少且使用率高，考虑预热
			float usageRate = (float)ActiveCount / TotalCapacity;
			return _inactiveObjects.Count < _minRetainCount / 2 && usageRate > 0.7f;
		}

		/// <summary>
		/// 计算预热数量
		/// </summary>
		/// <returns>预热数量</returns>
		private int CalculatePrewarmCount()
		{
			// 预热到最小保留数量
			return _minRetainCount - _inactiveObjects.Count;
		}

		// 添加热池管理方法
		public void AdjustHotPoolCapacity(int newCapacity)
		{
			if (newCapacity < 0)
			{
				PoolLogger.Error($"[CustomObjectPool] 热池容量不能为负数: {newCapacity}");
				return;
			}

			// 如果新容量小于当前热池大小，将多余对象移到主池
			while (_hotPool.Count > newCapacity)
			{
				IPoolObject hotObj;
				if (_hotPool.TryDequeue(out hotObj))
				{
					T obj = hotObj as T;
					if (obj != null)
					{
						_inactiveObjects.Push(obj);
					}
				}
			}

			_hotPoolCapacity = newCapacity;
			PoolLogger.Info($"[CustomObjectPool] 热池容量已调整为 {_hotPoolCapacity}");
		}

		// 添加热池阈值设置方法
		public void SetHotPoolThreshold(float threshold)
		{
			_hotPoolThreshold = Mathf.Clamp01(threshold);
			PoolLogger.Info($"[CustomObjectPool] 热池阈值已设置为 {_hotPoolThreshold}");
		}

		// 优化热池优化方法
		public void OptimizeHotPool()
		{
			// 如果正在优化或者距离上次优化时间不够长，跳过
			if (_isOptimizingHotPool || Time.time - _lastHotPoolOptimizeTime < HOT_POOL_OPTIMIZE_INTERVAL)
			{
				return;
			}

			// 使用锁确保线程安全
			lock (_lockObject)
			{
				try
				{
					_isOptimizingHotPool = true;
					float startTime = Time.realtimeSinceStartup;

					// 先处理所有待更新的使用频率
					BatchUpdateObjectUsageFrequency();

					// 创建临时列表存储热池对象
					List<IPoolObject> tempHotPool = new List<IPoolObject>();

					// 清空当前热池，将所有对象放回主池
					IPoolObject hotObj;
					while (_hotPool.TryDequeue(out hotObj))
					{
						if (hotObj != null)
						{
							tempHotPool.Add(hotObj);
						}
					}

					// 获取所有可用对象的快照，避免多次操作栈
					List<T> availableObjects = new List<T>();
					T tempObj;
					while (_inactiveObjects.TryPop(out tempObj))
					{
						if (tempObj != null)
						{
							availableObjects.Add(tempObj);
						}
					}

					// 将热池对象也添加到可用对象列表中
					foreach (var obj in tempHotPool)
					{
						T typedObj = obj as T;
						if (typedObj != null)
						{
							availableObjects.Add(typedObj);
						}
					}

					// 使用更高效的方式筛选和排序
					Dictionary<T, float> frequencySnapshot = new Dictionary<T, float>();

					// 创建使用频率的快照，避免在排序过程中对原始集合的访问
					foreach (var obj in availableObjects)
					{
						float frequency = 0;
						if (_objectUsageFrequency.TryGetValue(obj, out frequency))
						{
							frequencySnapshot[obj] = frequency;
						}
						else
						{
							frequencySnapshot[obj] = 0;
						}
					}

					var candidateObjects = new List<KeyValuePair<T, float>>(availableObjects.Count);

					foreach (var obj in availableObjects)
					{
						float frequency = frequencySnapshot[obj];
						if (frequency > _hotPoolThreshold)
						{
							candidateObjects.Add(new KeyValuePair<T, float>(obj, frequency));
						}
						else
						{
							// 不符合热池条件的对象直接放回主池
							_inactiveObjects.Push(obj);
						}
					}

					// 只对符合条件的对象进行排序，减少排序开销
					if (candidateObjects.Count > 0)
					{
						// 快速排序
						candidateObjects.Sort((a, b) => b.Value.CompareTo(a.Value));

						// 取前N个放入热池
						int hotPoolCount = Mathf.Min(candidateObjects.Count, _hotPoolCapacity);
						for (int i = 0; i < hotPoolCount; i++)
						{
							_hotPool.Enqueue(candidateObjects[i].Key);
						}

						// 剩余的放回主池
						for (int i = hotPoolCount; i < candidateObjects.Count; i++)
						{
							_inactiveObjects.Push(candidateObjects[i].Key);
						}
					}

					float optimizeTime = (Time.realtimeSinceStartup - startTime) * 1000;
					_lastHotPoolOptimizeTime = Time.time;

					// 记录优化性能
					if (optimizeTime > 10.0f) // 如果优化时间超过10毫秒，记录日志
					{
						PoolLogger.Warning($"[CustomObjectPool] 热池优化耗时较长: {optimizeTime:F2}ms");
					}

					PoolLogger.Info($"[CustomObjectPool] 热池优化完成，热池大小: {_hotPool.Count}, 主池大小: {_inactiveObjects.Count}, 耗时: {optimizeTime:F2}ms");
				}
				catch (Exception e)
				{
					PoolLogger.Error($"[CustomObjectPool] 优化热池时出错: {e.Message}");
				}
				finally
				{
					_isOptimizingHotPool = false;
				}
			}
		}

		// 添加扩展池的方法
		private void ExpandPool(int expandAmount)
		{
			if (_maxCount >= int.MaxValue - expandAmount)
			{
				expandAmount = int.MaxValue - _maxCount;
			}

			if (expandAmount <= 0)
			{
				return;
			}

			int oldMaxCount = _maxCount;
			_maxCount += expandAmount;

			Log.Info($"[CustomObjectPool] 扩展对象池 {_poolType}，容量从 {oldMaxCount} 增加到 {_maxCount}");

			// 预热新增加的对象
			Prewarm(expandAmount);
		}

		/// <summary>
		/// 清理对象池
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		public int Cleanup(int count = 0)
		{
			// 首先清理过期对象
			int cleanedCount = CleanupExpiredObjects(count);

			// 如果还需要清理更多对象，则清理多余对象
			if (count > 0 && cleanedCount < count)
			{
				cleanedCount += CleanupExcessObjects(count - cleanedCount);
			}

			return cleanedCount;
		}

		/// <summary>
		/// 清理对象池至最小保留数量
		/// </summary>
		/// <returns>实际清理的数量</returns>
		public int CleanupToMinimum()
		{
			int cleanupCount = _inactiveObjects.Count - _minRetainCount;
			if (cleanupCount <= 0)
			{
				return 0;
			}

			return CleanupExcessObjects(cleanupCount);
		}

		/// <summary>
		/// 设置对象池优先级
		/// </summary>
		/// <param name="priority">优先级</param>
		public void SetPriority(EnumPoolPriority priority)
		{
			// Implementation for setting pool priority
		}

		/// <summary>
		/// 获取对象池优先级
		/// </summary>
		/// <returns>优先级</returns>
		public EnumPoolPriority GetPriority()
		{
			// Default implementation
			return EnumPoolPriority.Medium;
		}

		private void UpdateObjectUsageFrequency(IPoolObject obj)
		{
			if (obj == null)
			{
				return;
			}

			// 使用锁确保线程安全
			lock (_lockObject)
			{
				// 将对象添加到待更新队列，而不是立即更新
				// 这样可以减少频繁更新带来的性能开销
				_pendingUsageUpdates.Enqueue(obj);

				// 如果队列过大或者距离上次更新时间已经足够长，执行批量更新
				if (_pendingUsageUpdates.Count >= USAGE_UPDATE_BATCH_SIZE ||
					Time.time - _lastUsageUpdateTime >= USAGE_UPDATE_INTERVAL)
				{
					BatchUpdateObjectUsageFrequency();
				}
			}
		}

		// 添加批量更新方法
		private void BatchUpdateObjectUsageFrequency()
		{
			if (_pendingUsageUpdates.Count == 0)
			{
				return;
			}

			// 使用锁确保线程安全
			lock (_lockObject)
			{
				float startTime = Time.realtimeSinceStartup;
				int updateCount = 0;

				// 限制每次更新的数量，避免卡顿
				int maxUpdatesPerBatch = Mathf.Min(_pendingUsageUpdates.Count, USAGE_UPDATE_BATCH_SIZE);

				for (int i = 0; i < maxUpdatesPerBatch; i++)
				{
					if (_pendingUsageUpdates.Count == 0)
					{
						break;
					}

					IPoolObject obj = _pendingUsageUpdates.Dequeue();
					if (obj != null)
					{
						// 更新使用频率，使用衰减模型
						float currentFrequency;
						if (!_objectUsageFrequency.TryGetValue(obj, out currentFrequency))
						{
							currentFrequency = 0;
						}

						float newFrequency = currentFrequency * 0.8f + 0.2f;
						_objectUsageFrequency[obj] = newFrequency;
						updateCount++;
					}
				}

				_lastUsageUpdateTime = Time.time;

				// 记录更新性能
				float updateTime = (Time.realtimeSinceStartup - startTime) * 1000;
				if (updateTime > 5.0f) // 如果更新时间超过5毫秒，记录日志
				{
					PoolLogger.Warning($"[CustomObjectPool] 批量更新对象使用频率耗时较长: {updateTime:F2}ms，更新了 {updateCount} 个对象");
				}
			}
		}
	}
}


