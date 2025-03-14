/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：PoolStatus
 * 
 * 创建者：Frandys
 * 创建时间：3/14/2025 1:45:34 PM
 * 描述：
 *----------------------------------------------------------------*/


namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池状态
	/// </summary>
	public class PoolStatus
	{
		private string poolType;
		private int activeCount;
		private int availableCount;
		private int totalCapacity;
		private int maxCapacity;
		private float usageRatio;

		public PoolStatus() { }
		public PoolStatus(string poolType, int activeCount, int availableCount, int totalCapacity, int maxCapacity, float usageRatio)
		{
			this.poolType = poolType;
			this.activeCount = activeCount;
			this.availableCount = availableCount;
			this.totalCapacity = totalCapacity;
			this.maxCapacity = maxCapacity;
			this.usageRatio = usageRatio;
		}

		/// <summary>
		/// 对象池类型
		/// </summary>
		public string PoolType { get => poolType; set => poolType = value; }

		/// <summary>
		/// 当前活跃对象数量
		/// </summary>
		public int ActiveCount { get => activeCount; set => activeCount = value; }

		/// <summary>
		/// 可用对象数量
		/// </summary>
		public int AvailableCount { get => availableCount; set => availableCount = value; }

		/// <summary>
		/// 总容量
		/// </summary>
		public int TotalCapacity { get => totalCapacity; set => totalCapacity = value; }

		/// <summary>
		/// 最大容量
		/// </summary>
		public int MaxCapacity { get => maxCapacity; set => maxCapacity = value; }

		/// <summary>
		/// 使用率
		/// </summary>
		public float UsageRatio { get => usageRatio; set => usageRatio = value; }
	}
}
