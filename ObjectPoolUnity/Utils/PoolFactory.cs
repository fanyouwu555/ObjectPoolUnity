/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：PoolFactory
 * 
 * 创建者：Frandys
 * 创建时间：3/6/2025 6:02:05 PM
 * 描述：对象池工厂类，负责创建不同类型的对象池
 *----------------------------------------------------------------*/


using System;
using System.Collections.Generic;
using UnityEngine;

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池工厂类
	/// 负责创建不同类型的对象池
	/// </summary>
	public static class PoolFactory
	{
		// 缓存已创建的对象池类型
		private static readonly Dictionary<Type, Type> _poolTypeCache = new Dictionary<Type, Type>();
		
		// 缓存委托创建器
		private static readonly Dictionary<Type, Func<PoolPrefabConfig, IPoolManager>> _poolCreatorCache = 
			new Dictionary<Type, Func<PoolPrefabConfig, IPoolManager>>();

		/// <summary>
		/// 注册对象池创建器
		/// </summary>
		/// <typeparam name="T">池对象组件类型</typeparam>
		/// <param name="creator">创建器委托</param>
		public static void RegisterPoolCreator<T>(Func<PoolPrefabConfig, IPoolManager> creator) where T : Component, IPoolObject
		{
			_poolCreatorCache[typeof(T)] = creator;
		}

		/// <summary>
		/// 创建对象池
		/// </summary>
		/// <param name="componentType">池对象组件类型</param>
		/// <param name="config">对象池配置</param>
		/// <returns>对象池实例</returns>
		public static IPoolManager Create(Type componentType, PoolPrefabConfig config)
		{
			// 验证参数
			ValidateConfig(config);
			ValidateComponentType(componentType);

			try
			{
				// 尝试使用缓存的创建器
				if (_poolCreatorCache.TryGetValue(componentType, out var creator))
				{
					return creator(config);
				}

				// 尝试使用缓存的类型
				Type poolType;
				if (!_poolTypeCache.TryGetValue(componentType, out poolType))
				{
					poolType = typeof(CustomObjectPool<>).MakeGenericType(componentType);
					_poolTypeCache[componentType] = poolType;
				}

				// 实例化对象池
				var pool = (IPoolManager)Activator.CreateInstance(poolType);
				pool.Init(config.poolTp, config.prefab, config.initialCount, config.maxCount);

				Debug.Log($"[PoolFactory] 创建对象池: {config.poolTp}，组件类型: {componentType.Name}");

				return pool;
			}
			catch (Exception e)
			{
				Debug.LogError($"[PoolFactory] 创建对象池失败: {e.Message}\n{e.StackTrace}");
				throw new InvalidOperationException($"创建对象池失败: {e.Message}", e);
			}
		}

		/// <summary>
		/// 验证对象池配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		private static void ValidateConfig(PoolPrefabConfig config)
		{
			if (config == null)
			{
				throw new ArgumentNullException(nameof(config), "对象池配置不能为空");
			}

			if (config.prefab == null)
			{
				throw new ArgumentNullException(nameof(config.prefab), $"类型 {config.poolTp} 的预制体不能为空");
			}

			if (config.initialCount < 0)
			{
				throw new ArgumentException($"类型 {config.poolTp} 的初始数量不能小于0", nameof(config.initialCount));
			}

			if (config.maxCount < config.initialCount)
			{
				throw new ArgumentException($"类型 {config.poolTp} 的最大数量不能小于初始数量", nameof(config.maxCount));
			}

			if (config.minRetainCount > config.initialCount)
			{
				throw new ArgumentException($"类型 {config.poolTp} 的最小保留数量不能大于初始数量", nameof(config.minRetainCount));
			}
		}

		/// <summary>
		/// 验证组件类型
		/// </summary>
		/// <param name="componentType">组件类型</param>
		private static void ValidateComponentType(Type componentType)
		{
			if (componentType == null)
			{
				throw new ArgumentNullException(nameof(componentType), "组件类型不能为空");
			}

			// 检查是否是组件类型
			if (!typeof(Component).IsAssignableFrom(componentType))
			{
				throw new ArgumentException($"类型 {componentType.Name} 不是 Component 类型", nameof(componentType));
			}

			// 检查是否实现了 IPoolObject 接口
			if (!typeof(IPoolObject).IsAssignableFrom(componentType))
			{
				throw new ArgumentException($"类型 {componentType.Name} 未实现 IBasePoolObject 接口", nameof(componentType));
			}
		}

		/// <summary>
		/// 创建对象池（泛型版本）
		/// </summary>
		/// <typeparam name="T">池对象组件类型</typeparam>
		/// <param name="config">对象池配置</param>
		/// <returns>对象池实例</returns>
		public static IPoolManager Create<T>(PoolPrefabConfig config) where T : Component, IPoolObject
		{
			// 尝试使用缓存的创建器
			if (_poolCreatorCache.TryGetValue(typeof(T), out var creator))
			{
				return creator(config);
			}

			// 创建并缓存新的创建器
			creator = (cfg) =>
			{
				var pool = new CustomObjectPool<T>();
				pool.Init(cfg.poolTp, cfg.prefab, cfg.initialCount, cfg.maxCount);
				return pool;
			};
			_poolCreatorCache[typeof(T)] = creator;

			return creator(config);
		}

		/// <summary>
		/// 从预制体创建对象池
		/// </summary>
		/// <param name="tp">类型</param>
		/// <param name="prefab">预制体</param>
		/// <param name="initialCount">初始数量</param>
		/// <param name="maxCount">最大数量</param>
		/// <returns>对象池实例</returns>
		public static IPoolManager CreateFromPrefab(string tp, GameObject prefab, int initialCount = 10, int maxCount = 100)
		{
			if (prefab == null)
			{
				throw new ArgumentNullException(nameof(prefab), "预制体不能为空");
			}

			// 获取预制体上的 IPoolObject 组件
			var component = prefab.GetComponent<IPoolObject>();
			if (component == null)
			{
				throw new Exception($"预制体 {prefab.name} 缺少 IBasePoolObject 组件");
			}

			// 创建临时配置
			var config = new PoolPrefabConfig(
				tp,
				prefab,
				initialCount,
				maxCount,
				0
			);

			return Create(component.GetType(), config);
		}

		/// <summary>
		/// 清除类型缓存
		/// </summary>
		public static void ClearCache()
		{
			_poolTypeCache.Clear();
			_poolCreatorCache.Clear();
		}
	}
}
