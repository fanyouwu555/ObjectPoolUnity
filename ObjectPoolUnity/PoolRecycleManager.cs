/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：PoolRecycleManager
 * 
 * 创建者：Frandys
 * 修改者：Claude AI
 * 创建时间：12/24/2024 10:33:35 AM
 * 描述：对象池回收管理器，负责对象池的回收和清理策略
 *----------------------------------------------------------------*/

using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Profiling;
using UnityGameFramework.Runtime;
using System.Linq;

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池回收管理器
	/// 负责对象池的回收和清理策略
	/// </summary>
	public class PoolRecycleManager : MonoBehaviour, IPoolRecycleManager
	{
		// 对象池字典引用
		private Dictionary<string, IPoolManager> _pools;

		// 批量回收阈值
		private int _batchThreshold = 10;

		// 清理检查间隔（秒）
		private float _cleanupInterval = 60f;

		// 上次清理时间
		private float _lastCleanupTime = 0f;

		// 是否启用定期清理
		private bool _enablePeriodicCleanup = true;

		// 待回收对象队列
		private Queue<IPoolObject> _recycleQueue = new Queue<IPoolObject>();

		// 回收队列锁
		private readonly object _recycleLock = new object();

		// 低内存阈值（系统内存使用率）
		private float _lowMemoryThreshold = 0.85f;

		// 危急内存阈值
		private float _criticalMemoryThreshold = 0.92f;

		// 内存检查间隔（秒）
		private float _memoryCheckInterval = 10f;

		// 上次内存检查时间
		private float _lastMemoryCheckTime = 0f;

		// 是否启用内存压力自适应
		private bool _enableMemoryPressureAdaptation = true;

		// 当前内存压力级别
		private EnumMemoryPressureLevel _currentMemoryPressureLevel = EnumMemoryPressureLevel.Low;

		// 对象池优先级字典
		private Dictionary<string, EnumPoolPriority> _poolPriorities = new Dictionary<string, EnumPoolPriority>();



		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="pools">对象池字典</param>
		/// <param name="eventManager">事件管理器</param>
		public void init(Dictionary<string, IPoolManager> pools)
		{
			_pools = pools;
			PoolLogger.Info("[PoolRecycleManager] 初始化完成");
		}

		/// <summary>
		/// 设置批量回收阈值
		/// </summary>
		/// <param name="threshold">阈值</param>
		public void SetBatchThreshold(int threshold)
		{
			if (threshold <= 0)
			{
				PoolLogger.Warning("[PoolRecycleManager] 批量回收阈值必须大于0，设置为默认值10");
				_batchThreshold = 10;
				return;
			}

			_batchThreshold = threshold;
			PoolLogger.Info($"[PoolRecycleManager] 设置批量回收阈值为 {threshold}");
		}

		/// <summary>
		/// 设置清理间隔
		/// </summary>
		/// <param name="interval">间隔时间（秒）</param>
		public void SetCleanupInterval(float interval)
		{
			if (interval <= 0)
			{
				PoolLogger.Warning("[PoolRecycleManager] 清理间隔必须大于0，设置为默认值60秒");
				_cleanupInterval = 60f;
				return;
			}

			_cleanupInterval = interval;
			PoolLogger.Info($"[PoolRecycleManager] 设置清理间隔为 {interval}秒");
		}

		/// <summary>
		/// 启用定期清理
		/// </summary>
		/// <param name="enable">是否启用</param>
		public void EnablePeriodicCleanup(bool enable)
		{
			_enablePeriodicCleanup = enable;
			PoolLogger.Info($"[PoolRecycleManager] {(enable ? "启用" : "禁用")}定期清理");
		}

		/// <summary>
		/// 设置定期清理（别名方法，与EnablePeriodicCleanup功能相同）
		/// </summary>
		/// <param name="enable">是否启用</param>
		public void SetPeriodicCleanup(bool enable)
		{
			EnablePeriodicCleanup(enable);
		}

		/// <summary>
		/// 检查定期清理
		/// </summary>
		/// <param name="currentTime">当前时间</param>
		public void CheckPeriodicCleanup(float currentTime)
		{
			if (!_enablePeriodicCleanup)
				return;

			if (currentTime - _lastCleanupTime >= _cleanupInterval)
			{
				_lastCleanupTime = currentTime;
				CleanupPool();

				// 检查内存压力
				if (_enableMemoryPressureAdaptation && currentTime - _lastMemoryCheckTime >= _memoryCheckInterval)
				{
					_lastMemoryCheckTime = currentTime;
					CheckMemoryPressure();
				}
			}

			// 处理回收队列
			ProcessRecycleQueue();
		}

		/// <summary>
		/// 检查内存压力
		/// </summary>
		private void CheckMemoryPressure()
		{
			// 获取系统内存使用率
			float memoryUsage = GetSystemMemoryUsage();
			EnumMemoryPressureLevel newLevel = _currentMemoryPressureLevel;

			if (memoryUsage >= _criticalMemoryThreshold)
			{
				newLevel = EnumMemoryPressureLevel.Critical;
			}
			else if (memoryUsage >= _lowMemoryThreshold)
			{
				newLevel = EnumMemoryPressureLevel.High;
			}
			else
			{
				newLevel = EnumMemoryPressureLevel.Low;
			}

			// 如果内存压力级别变化，执行相应操作
			if (newLevel != _currentMemoryPressureLevel)
			{
				_currentMemoryPressureLevel = newLevel;

				switch (newLevel)
				{
					case EnumMemoryPressureLevel.Critical:
						PoolLogger.Warning("[PoolRecycleManager] 检测到危急内存压力，执行紧急清理");
						ForceCleanupAllPools(true);
						break;
					case EnumMemoryPressureLevel.High:
						PoolLogger.Warning("[PoolRecycleManager] 检测到高内存压力，执行额外清理");
						CleanupPool(null, 0);
						break;
					default:
						PoolLogger.Info("[PoolRecycleManager] 内存压力恢复正常");
						break;
				}
			}
		}

		/// <summary>
		/// 获取系统内存使用率
		/// </summary>
		/// <returns>内存使用率（0-1）</returns>
		private float GetSystemMemoryUsage()
		{
			// 使用Unity的Profiler获取内存使用情况
			// 注意：这只是一个近似值，不同平台可能需要不同的实现
			long totalMemory = Profiler.GetTotalAllocatedMemoryLong();
			long reservedMemory = Profiler.GetTotalReservedMemoryLong();

			// 避免除零错误
			if (reservedMemory <= 0)
				return 0f;

			return (float)totalMemory / reservedMemory;
		}

		/// <summary>
		/// 处理回收队列
		/// </summary>
		private void ProcessRecycleQueue()
		{
			int count = 0;

			lock (_recycleLock)
			{
				while (count < _batchThreshold && _recycleQueue.Count > 0)
				{
					IPoolObject obj = _recycleQueue.Dequeue();
					if (obj == null)
						continue;

					try
					{
						string poolType = obj.PoolType;
						if (_pools.TryGetValue(poolType, out var pool))
						{
							pool.Release(obj);
							count++;
						}
						else
						{
							PoolLogger.Warning($"[PoolRecycleManager] 找不到对象池 {obj.PoolType}，无法回收对象");
						}
					}
					catch (Exception e)
					{
						PoolLogger.Error($"[PoolRecycleManager] 回收对象时发生错误: {e.Message}");
					}
				}
			}

			if (count > 0)
			{
				Log.Debug($"[PoolRecycleManager] 批量回收了 {count} 个对象");
			}
		}

		/// <summary>
		/// 添加对象到回收队列
		/// </summary>
		/// <param name="obj">对象</param>
		public void AddToRecycleQueue(IPoolObject obj)
		{
			if (obj == null)
				return;

			lock (_recycleLock)
			{
				_recycleQueue.Enqueue(obj);
			}
		}

		/// <summary>
		/// 设置池优先级
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <param name="priority">优先级</param>
		public void SetPoolPriority(string poolType, EnumPoolPriority priority)
		{
			if (string.IsNullOrEmpty(poolType))
				return;

			_poolPriorities[poolType] = priority;
			PoolLogger.Info($"[PoolRecycleManager] 设置对象池 {poolType} 的优先级为 {priority}");
		}

		/// <summary>
		/// 获取池优先级
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>优先级</returns>
		public EnumPoolPriority GetPoolPriority(string poolType)
		{
			if (string.IsNullOrEmpty(poolType))
			{
				return EnumPoolPriority.Medium;
			}

			if (_poolPriorities.TryGetValue(poolType, out var priority))
			{
				return priority;
			}

			return EnumPoolPriority.Medium; // 默认为中等优先级
		}

		/// <summary>
		/// 清理对象池
		/// </summary>
		/// <param name="poolType">对象池类型，如果为空则清理所有对象池</param>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		public int CleanupPool(string poolType = null, int count = 0)
		{
			int totalCleaned = 0;

			if (string.IsNullOrEmpty(poolType))
			{
				// 按优先级排序对象池
				var sortedPools = _pools.Keys
					.Select(p => new { PoolType = p, Priority = GetPoolPriority(p) })
					.OrderByDescending(p => (int)p.Priority)
					.Select(p => p.PoolType)
					.ToList();

				foreach (var type in sortedPools)
				{
					if (_pools.TryGetValue(type, out var pool))
					{
						int cleaned = pool.Cleanup(count);
						totalCleaned += cleaned;

						if (cleaned > 0)
						{
							PoolLogger.Info($"[PoolRecycleManager] 清理对象池 {type}，回收了 {cleaned} 个对象");
						}
					}
				}
			}
			else if (_pools.TryGetValue(poolType, out var pool))
			{
				int cleaned = pool.Cleanup(count);
				totalCleaned = cleaned;

				if (cleaned > 0)
				{
					PoolLogger.Info($"[PoolRecycleManager] 清理对象池 {poolType}，回收了 {cleaned} 个对象");
				}
			}

			return totalCleaned;
		}

		/// <summary>
		/// 强制清理所有对象池
		/// </summary>
		/// <param name="retainMinimum">是否只保留最小数量</param>
		public void ForceCleanupAllPools(bool retainMinimum = false)
		{
			int totalCleaned = 0;

			foreach (var pair in _pools)
			{
				int cleaned = retainMinimum ?
					pair.Value.CleanupToMinimum() :
					pair.Value.Cleanup(int.MaxValue);

				totalCleaned += cleaned;

				if (cleaned > 0)
				{
				}
			}

			PoolLogger.Info($"[PoolRecycleManager] 强制清理所有对象池，共回收 {totalCleaned} 个对象");
		}
	}
}
