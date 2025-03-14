/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：IPoolConfigManager
 * 
 * 创建者：Frandys
 * 创建时间：3/13/2025 2:58:48 PM
 * 描述：
 *----------------------------------------------------------------*/


using System.Collections.Generic;

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池配置管理器接口
	/// </summary>
	public interface IPoolConfigManager
	{
		/// <summary>
		/// 获取对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>对象池配置</returns>
		PoolPrefabConfig GetConfig(string poolType);

		/// <summary>
		/// 添加对象池配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		void AddConfig(PoolPrefabConfig config);

		/// <summary>
		/// 更新对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <param name="config">新配置</param>
		void UpdateConfig(string poolType, PoolPrefabConfig config);

		/// <summary>
		/// 移除对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>是否成功移除</returns>
		bool RemoveConfig(string poolType);

		/// <summary>
		/// 获取所有对象池配置
		/// </summary>
		/// <returns>所有对象池配置</returns>
		List<PoolPrefabConfig> GetAllConfigs();
	}
}
