/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：EnumMemoryPressureLevel
 * 
 * 创建者：Frandys
 * 创建时间：3/13/2025 3:25:25 PM
 * 描述：
 *----------------------------------------------------------------*/


namespace BEWGame.Pool
{
	/// <summary>
	/// 内存压力级别
	/// </summary>
	public enum EnumMemoryPressureLevel
	{
		/// <summary>
		/// 内存压力低
		/// </summary>
		Low,

		/// <summary>
		/// 内存压力中等
		/// </summary>
		Medium,

		/// <summary>
		/// 内存压力高
		/// </summary>
		High,

		/// <summary>
		/// 内存压力严重
		/// </summary>
		Critical
	}
}
