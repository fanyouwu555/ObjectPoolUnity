/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：IPoolRecycleManager
 * 
 * 创建者：Frandys
 * 创建时间：3/13/2025 2:59:15 PM
 * 描述：
 *----------------------------------------------------------------*/


namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池回收管理器接口
	/// </summary>
	public interface IPoolRecycleManager
	{
		/// <summary>
		/// 设置批量回收阈值
		/// </summary>
		/// <param name="threshold">阈值</param>
		void SetBatchThreshold(int threshold);

		/// <summary>
		/// 设置清理间隔
		/// </summary>
		/// <param name="interval">间隔时间（秒）</param>
		void SetCleanupInterval(float interval);

		/// <summary>
		/// 启用定期清理
		/// </summary>
		/// <param name="enable">是否启用</param>
		void EnablePeriodicCleanup(bool enable);

		/// <summary>
		/// 清理对象池
		/// </summary>
		/// <param name="poolType">对象池类型，如果为空则清理所有对象池</param>
		/// <param name="count">清理数量，如果为0则自动计算</param>
		/// <returns>实际清理的数量</returns>
		int CleanupPool(string poolType = null, int count = 0);

		/// <summary>
		/// 检查定期清理
		/// </summary>
		/// <param name="currentTime">当前时间</param>
		void CheckPeriodicCleanup(float currentTime);

		/// <summary>
		/// 强制清理所有对象池
		/// </summary>
		/// <param name="retainMinimum">是否只保留最小数量</param>
		void ForceCleanupAllPools(bool retainMinimum = false);

		/// <summary>
		/// 设置池优先级
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <param name="priority">优先级</param>
		void SetPoolPriority(string poolType, EnumPoolPriority priority);

		/// <summary>
		/// 获取池优先级
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>优先级</returns>
		EnumPoolPriority GetPoolPriority(string poolType);

		/// <summary>
		/// 添加对象到回收队列
		/// </summary>
		/// <param name="obj">对象</param>
		void AddToRecycleQueue(IPoolObject obj);
	}
}
