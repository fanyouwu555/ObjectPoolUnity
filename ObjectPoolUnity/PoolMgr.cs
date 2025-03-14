/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：PoolMgr
 * 
 * 创建者：Frandys
 * 修改者：Claude AI
 * 创建时间：12/24/2024 10:33:35 AM
 * 描述：对象池管理器，负责管理所有类型的对象池
 *----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace BEWGame.Pool
{
	/// </summary>
	public class PoolMgr : GameFrameworkComponent
	{
		[SerializeField, Tooltip("对象池配置文件")]
		private ObjectPoolConfig _config;

		[SerializeField, Tooltip("是否在初始化时自动创建所有对象池")]
		private bool _autoInitializeAllPools = true;

		[SerializeField, Tooltip("对象池容量警告阈值(0.0-1.0)"), Range(0.7f, 0.95f)]
		private float _poolCapacityWarningThreshold = 0.8f;

		[SerializeField, Tooltip("是否启用自动扩展池容量")]
		private bool _enableAutoExpand = true;

		[SerializeField, Tooltip("自动扩展的增量百分比"), Range(0.1f, 0.5f)]
		private float _autoExpandRatio = 0.25f;

		[SerializeField, Tooltip("批量回收的缓存阈值")]
		private int _batchRecycleThreshold = 10;

		[SerializeField, Tooltip("是否启用定期清理")]
		private bool _enablePeriodicCleanup = true;

		[SerializeField, Tooltip("清理检查间隔(秒)")]
		private float _cleanupCheckInterval = 60f;



		// 对象池根节点
		private Transform _poolRoot;

		// 对象池字典，按类型存储
		private Dictionary<string, IPoolManager> _pools = new Dictionary<string, IPoolManager>();

		// 辅助管理器
		private PoolConfigManager _configManager;
		private PoolRecycleManager _recycleManager;




		/// <summary>
		/// 初始化
		/// </summary>
		protected override void Awake()
		{
			base.Awake();

			CreateObjectPoolRoot();

			// 初始化辅助管理器
			_configManager = GetComponent<PoolConfigManager>();
			_configManager.init(_config);
			_recycleManager = GetComponent<PoolRecycleManager>();
			_recycleManager.init(_pools);


			// 设置回收管理器参数
			_recycleManager.SetBatchThreshold(_batchRecycleThreshold);
			_recycleManager.SetCleanupInterval(_cleanupCheckInterval);
			_recycleManager.SetPeriodicCleanup(_enablePeriodicCleanup);

			// 如果配置了自动初始化所有对象池
			if (_autoInitializeAllPools && _config != null)
			{
				InitializeAllPools();
			}

			Log.Info("[PoolMgr] 对象池管理器初始化完成");
		}

		//添加根节点
		private void CreateObjectPoolRoot()
		{
			if (_poolRoot.IsUnityNull() is false)
			{
				return;
			}
			// 创建对象池根节点
			GameObject poolRootObj = new GameObject("[ObjectPools]");
			_poolRoot = poolRootObj.transform;
			_poolRoot.SetParent(transform);
		}



		/// <summary>
		/// 更新
		/// </summary>
		private void Update()
		{
			// 更新回收管理器
			_recycleManager?.CheckPeriodicCleanup(Time.time);

		}

		/// <summary>
		/// 初始化所有对象池
		/// </summary>
		private void InitializeAllPools()
		{
			if (_config == null || _config.prefabConfigs == null)
			{
				PoolLogger.Error("[PoolMgr] 对象池配置为空，无法初始化对象池");
				return;
			}

			int successCount = 0;
			int failCount = 0;

			foreach (var config in _config.prefabConfigs)
			{
				try
				{
					// 检查预制体是否有效
					if (ValidatePoolConfig(config) is false)
					{
						failCount++;
						continue;
					}
					var component = config.prefab.GetComponent<IPoolObject>();
					// 创建对象池
					var pool = PoolFactory.Create(component.GetType(), config);
					pool.PoolRoot.SetParent(_poolRoot);
					// 将对象池添加到字典
					_pools[config.poolTp] = pool;
					if (config.initialCount > 0)
					{
						PrewarmPool(config.poolTp, config.initialCount);
					}
					successCount++;


				}
				catch (Exception e)
				{
					PoolLogger.Error($"[PoolMgr] 创建对象池失败: {config.poolTp}, {e}");
					failCount++;
				}
			}

			PoolLogger.Info($"[PoolMgr] 初始化对象池完成，成功: {successCount}，失败: {failCount}");
		}




		private bool ValidatePoolConfig(PoolPrefabConfig config)
		{
			return _configManager.ValidateConfig(config);
		}

		#region 公共API



		/// <summary>
		/// 获取对象
		/// </summary>
		/// <param name="poolTp">对象池类型</param>
		/// <returns>池对象</returns>
		public IPoolObject Spawn(string poolTp)
		{
			if (string.IsNullOrEmpty(poolTp))
			{
				PoolLogger.Error("[PoolMgr] 无法获取对象，对象池类型为空");
				return null;
			}

			if (!_pools.TryGetValue(poolTp, out var pool))
			{
				// 尝试延迟创建对象池
				if (TryCreatePool(poolTp))
				{
					pool = _pools[poolTp];
				}
				else
				{
					PoolLogger.Error($"[PoolMgr] 无法获取对象，对象池不存在: {poolTp}");
					return null;
				}
			}

			try
			{
				var obj = pool.Get();
				if (obj != null)
				{
					// 增加引用计数
					obj.AddReference();

					// 检查容量警告
					CheckPoolCapacity(pool);
				}
				return obj;
			}
			catch (Exception e)
			{
				PoolLogger.Error($"[PoolMgr] 获取对象失败: {poolTp}, {e}");
				return null;
			}
		}

		/// <summary>
		/// 获取对象（泛型版本）
		/// </summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <param name="poolTp">对象池类型</param>
		/// <returns>池对象</returns>
		public T Spawn<T>(string poolTp) where T : Component, IPoolObject
		{
			var obj = Spawn(poolTp);
			if (obj == null)
			{
				return null;
			}

			return obj as T;
		}

		/// <summary>
		/// 回收对象
		/// </summary>
		/// <param name="obj">要回收的对象</param>
		public void Despawn(IPoolObject obj)
		{
			if (obj == null)
			{
				PoolLogger.Warning("[PoolMgr] 无法回收对象，对象为空");
				return;
			}

			string poolTp = obj.PoolType;
			if (string.IsNullOrEmpty(poolTp))
			{
				PoolLogger.Warning($"[PoolMgr] 无法回收对象，对象池类型为空: {obj.gameObject.name}");
				return;
			}

			if (!_pools.TryGetValue(poolTp, out var pool))
			{
				PoolLogger.Warning($"[PoolMgr] 无法回收对象，对象池不存在: {poolTp}");
				return;
			}

			// 检查引用计数，如果引用计数不为0，则不回收
			if (obj.ReferenceCount > 0)
			{
				PoolLogger.Info($"[PoolMgr] 对象 {obj.gameObject.name} 引用计数为 {obj.ReferenceCount}，暂不回收");
				return;
			}

			try
			{
				pool.Release(obj);

			}
			catch (Exception e)
			{
				PoolLogger.Error($"[PoolMgr] 回收对象失败: {poolTp}, {e}");
			}
		}

		/// <summary>
		/// 回收所有对象
		/// </summary>
		/// <param name="poolTp">对象池类型，如果为空则回收所有对象池的对象</param>
		public void DespawnAll(string poolTp = null)
		{
			if (string.IsNullOrEmpty(poolTp))
			{
				// 回收所有对象池的对象
				foreach (var pool in _pools.Values)
				{
					try
					{
						pool.ReleaseAll();
					}
					catch (Exception e)
					{
						PoolLogger.Error($"[PoolMgr] 回收所有对象失败: {pool.PoolType}, {e}");
					}
				}
			}
			else
			{
				// 回收指定对象池的对象
				if (_pools.TryGetValue(poolTp, out var pool))
				{
					try
					{
						pool.ReleaseAll();
					}
					catch (Exception e)
					{
						PoolLogger.Error($"[PoolMgr] 回收所有对象失败: {poolTp}, {e}");
					}
				}
				else
				{
					PoolLogger.Warning($"[PoolMgr] 无法回收所有对象，对象池不存在: {poolTp}");
				}
			}
		}

		/// <summary>
		/// 创建对象池
		/// </summary>
		/// <param name="poolTp">对象池类型</param>
		/// <param name="prefab">预制体</param>
		/// <param name="initialCount">初始数量</param>
		/// <param name="maxCount">最大数量</param>
		/// <returns>是否创建成功</returns>
		public bool CreatePool(string poolTp, GameObject prefab, int initialCount, int maxCount)
		{
			if (string.IsNullOrEmpty(poolTp))
			{
				PoolLogger.Error("[PoolMgr] 无法创建对象池，对象池类型为空");
				return false;
			}

			if (prefab == null)
			{
				PoolLogger.Error($"[PoolMgr] 无法创建对象池，预制体为空: {poolTp}");
				return false;
			}

			if (_pools.ContainsKey(poolTp))
			{
				PoolLogger.Warning($"[PoolMgr] 对象池已存在: {poolTp}");
				return false;
			}

			try
			{
				// 检查预制体是否包含必要组件
				var component = prefab.GetComponent<IPoolObject>();
				if (component == null)
				{
					throw new Exception(
						$"预制体 {prefab.name} 缺少 IPoolObject 组件");
				}

				// 创建配置
				var config = new PoolPrefabConfig(poolTp, prefab, initialCount, maxCount);
				config.allowAutoExpand = _enableAutoExpand;
				config.autoExpandRatio = _autoExpandRatio;

				// 创建对象池
				var pool = PoolFactory.Create(component.GetType(), config);
				pool.PoolRoot.SetParent(_poolRoot);
				// 将对象池添加到字典
				_pools[poolTp] = pool;

				// 添加到配置
				_configManager.AddConfig(config);


				return true;
			}
			catch (Exception e)
			{
				PoolLogger.Error($"[PoolMgr] 创建对象池失败: {poolTp}, {e}");
				return false;
			}
		}

		/// <summary>
		/// 尝试创建对象池
		/// </summary>
		/// <param name="poolTp">对象池类型</param>
		/// <returns>是否创建成功</returns>
		private bool TryCreatePool(string poolTp)
		{
			if (string.IsNullOrEmpty(poolTp))
			{
				return false;
			}

			// 从配置中获取
			var config = _configManager.GetConfig(poolTp);
			if (config == null || config.prefab == null)
			{
				return false;
			}

			return CreatePool(poolTp, config.prefab, config.initialCount, config.maxCount);
		}

		/// <summary>
		/// 检查对象池容量
		/// </summary>
		/// <param name="pool">对象池</param>
		private void CheckPoolCapacity(IPoolManager pool)
		{
			if (pool == null)
			{
				return;
			}

			float capacityRatio = (float)pool.ActiveCount / pool.MaxCapacity;
			if (capacityRatio >= _poolCapacityWarningThreshold)
			{

				// 如果启用了自动扩展，尝试扩展对象池
				if (_enableAutoExpand && pool.ActiveCount >= pool.TotalCapacity * 0.9f)
				{
					int expandCount = Mathf.CeilToInt(pool.MaxCapacity * _autoExpandRatio);
					var config = _configManager.GetConfig(pool.PoolType);
					if (config != null)
					{
						config.maxCount += expandCount;
						pool.UpdateConfig(config);
						PoolLogger.Info($"[PoolMgr] 自动扩展对象池: {pool.PoolType}，新容量: {config.maxCount}");
					}
				}
			}
		}



		/// <summary>
		/// 获取对象池状态
		/// </summary>
		/// <param name="poolTp">对象池类型</param>
		/// <returns>对象池状态</returns>
		public PoolStatus GetPoolStatus(string poolTp)
		{
			if (string.IsNullOrEmpty(poolTp) || !_pools.TryGetValue(poolTp, out var pool))
			{
				return null;
			}

			return GetPoolStatus(pool);
		}

		/// <summary>
		/// 获取所有对象池状态
		/// </summary>
		/// <returns>所有对象池状态</returns>
		public List<PoolStatus> GetAllPoolStatus()
		{
			var result = new List<PoolStatus>();
			foreach (var pool in _pools.Values)
			{
				result.Add(GetPoolStatus(pool));
			}
			return result;
		}

		private PoolStatus GetPoolStatus(IPoolManager pool)
		{
			return new PoolStatus
			(
				poolType: pool.PoolType,
				activeCount: pool.ActiveCount,
				availableCount: pool.AvailableCount,
				totalCapacity: pool.TotalCapacity,
				maxCapacity: pool.MaxCapacity,
				usageRatio: pool.ActiveCount / (float)pool.MaxCapacity
			);
		}

		/// <summary>
		/// 预热对象池
		/// </summary>
		/// <param name="poolTp">对象池类型</param>
		/// <param name="count">预热数量</param>
		public void PrewarmPool(string poolTp, int count)
		{
			if (string.IsNullOrEmpty(poolTp) || !_pools.TryGetValue(poolTp, out var pool))
			{
				PoolLogger.Warning($"[PoolMgr] 无法预热对象池，对象池不存在: {poolTp}");
				return;
			}

			try
			{
				pool.Prewarm(count);
			}
			catch (Exception e)
			{
				PoolLogger.Error($"[PoolMgr] 预热对象池失败: {poolTp}, {e}");
			}
		}



		#endregion
	}


}


