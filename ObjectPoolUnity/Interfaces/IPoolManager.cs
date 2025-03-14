/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：IPoolManager
 * 
 * 创建者：Frandys
 * 修改者：Claude AI
 * 创建时间：3/6/2025 2:37:30 PM
 * 描述：对象池管理器接口，定义了对象池的基本操作
 *----------------------------------------------------------------*/

using System;
using UnityEngine;

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池管理器接口
	/// 定义了对象池的基本操作
	/// </summary>
	public interface IPoolManager : IDisposable
	{
		/// <summary>
		/// 初始化对象池
		/// </summary>
		/// /// <param name="tp">类型</param>
		/// <param name="prefab">预制体</param>
		/// <param name="initialCount">初始数量</param>
		/// <param name="maxCount">最大数量</param>
		void Init(string tp, GameObject prefab, int initialCount, int maxCount);

		/// <summary>
		/// 预生成对象
		/// </summary>
		/// <param name="count">预生成数量</param>
		void Prewarm(int count);


		/// <summary>
		/// 对象池根节点
		/// </summary>
		Transform PoolRoot { get; }

		/// <summary>
		/// 当前活跃对象数量
		/// </summary>
		int ActiveCount { get; }

		/// <summary>
		/// 对象池总容量
		/// </summary>
		int TotalCapacity { get; }

		/// <summary>
		/// 对象池可用对象数量
		/// </summary>
		int AvailableCount { get; }

		/// <summary>
		/// 获取非活跃对象数量
		/// </summary>
		/// <returns>非活跃对象数量</returns>
		int GetInactiveCount();

		/// <summary>
		/// 对象池最大容量
		/// </summary>
		int MaxCapacity { get; }

		/// <summary>
		/// 对象池类型
		/// </summary>
		string PoolType { get; }

		/// <summary>
		/// 获取对象池类型
		/// </summary>
		/// <returns>对象池类型</returns>
		string GetPoolType();

		/// <summary>
		/// 对象池是否已初始化
		/// </summary>
		bool IsInitialized { get; }

		/// <summary>
		/// 对象池是否已销毁
		/// </summary>
		bool IsDisposed { get; }

		/// <summary>
		/// 获取对象
		/// </summary>
		/// <returns>池对象</returns>
		IPoolObject Get();

		/// <summary>
		/// 获取对象（泛型版本）
		/// </summary>
		/// <typeparam name="T">对象类型</typeparam>
		/// <returns>池对象</returns>
		T Get<T>() where T : Component, IPoolObject;

		/// <summary>
		/// 回收对象
		/// </summary>
		/// <param name="obj">要回收的对象</param>
		void Release(IPoolObject obj);

		/// <summary>
		/// 回收所有对象
		/// </summary>
		void ReleaseAll();

		/// <summary>
		/// 清理过期对象
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		int CleanupExpiredObjects(int count = 0);

		/// <summary>
		/// 清理非活跃对象
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		int CleanupInactiveObjects(int count = 0);

		/// <summary>
		/// 清理对象池
		/// </summary>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		int Cleanup(int count = 0);

		/// <summary>
		/// 清理对象池至最小保留数量
		/// </summary>
		/// <returns>实际清理的数量</returns>
		int CleanupToMinimum();

		/// <summary>
		/// 设置对象池配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		void SetConfig(PoolPrefabConfig config);

		/// <summary>
		/// 获取对象池配置
		/// </summary>
		/// <returns>对象池配置</returns>
		PoolPrefabConfig GetConfig();

		/// <summary>
		/// 重置对象池
		/// </summary>
		void Reset();

		/// <summary>
		/// 暂停对象池
		/// </summary>
		void Pause();

		/// <summary>
		/// 恢复对象池
		/// </summary>
		void Resume();

		/// <summary>
		/// 更新对象池配置
		/// </summary>
		/// <param name="newConfig">新配置</param>
		void UpdateConfig(PoolPrefabConfig newConfig);

		/// <summary>
		/// 序列化池配置
		/// </summary>
		/// <returns>序列化的配置数据</returns>
		string SerializeConfig();

		/// <summary>
		/// 从序列化数据恢复配置
		/// </summary>
		/// <param name="configJson">序列化的配置数据</param>
		void DeserializeConfig(string configJson);

		/// <summary>
		/// 设置自动扩展
		/// </summary>
		/// <param name="enable">是否启用自动扩展</param>
		void SetAutoExpand(bool enable);

		/// <summary>
		/// 定期维护对象池
		/// </summary>
		/// <param name="force">是否强制执行</param>
		void Maintain(bool force = false);
	}
}