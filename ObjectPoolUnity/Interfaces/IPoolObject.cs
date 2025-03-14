/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：IPoolObject
 * 
 * 创建者：Claude AI
 * 创建时间：3/12/2024 4:55:35 PM
 * 描述：对象池对象接口定义
 *----------------------------------------------------------------*/

using UnityEngine;

namespace BEWGame.Pool
{

	/// <summary>
	/// 完整对象池对象接口
	/// </summary>
	public interface IPoolObject
	{

		/// <summary>
		/// 对象池类型
		/// </summary>
		string PoolType { get; set; }

		/// <summary>
		/// 对象被生成时调用
		/// </summary>
		void OnSpawn();

		/// <summary>
		/// 对象被回收时调用
		/// </summary>
		void OnDespawn();

		/// <summary>
		/// 游戏对象引用
		/// </summary>
		GameObject gameObject { get; }

		/// <summary>
		/// 对象是否活跃
		/// </summary>
		bool IsActive { get; }

		/// <summary>
		/// 对象是否可回收
		/// </summary>
		bool IsRecyclable { get; }

		/// <summary>
		/// 对象上次使用时间
		/// </summary>
		float LastUsedTime { get; set; }

		/// <summary>
		/// 对象上次激活时间
		/// </summary>
		float LastActiveTime { get; set; }

		/// <summary>
		/// 对象生命周期
		/// </summary>
		float Lifetime { get; set; }

		/// <summary>
		/// 对象优先级
		/// </summary>
		int Priority { get; set; }

		/// <summary>
		/// 对象引用计数
		/// </summary>
		int ReferenceCount { get; set; }

		/// <summary>
		/// 对象被创建时调用
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		void OnCreate(string poolType);

		/// <summary>
		/// 对象被销毁时调用
		/// </summary>
		void OnDestroy();

		/// <summary>
		/// 对象被重置时调用
		/// </summary>
		void OnReset();

		/// <summary>
		/// 增加引用计数
		/// </summary>
		void AddReference();

		/// <summary>
		/// 减少引用计数
		/// </summary>
		/// <returns>引用计数是否为0</returns>
		bool RemoveReference();
	}
}
