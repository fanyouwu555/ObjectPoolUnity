/*----------------------------------------------------------------
 * 版权所有 (c) 2025   保留所有权利。
 * 文件名：EnumPoolPriority
 * 
 * 创建者：Frandys
 * 创建时间：3/13/2025 3:00:24 PM
 * 描述：
 *----------------------------------------------------------------*/


namespace BEWGame.Pool
{
	public enum EnumPoolPriority
	{
		/// <summary>
		/// 低优先级（优先回收）
		/// </summary>
		Low = 0,

		/// <summary>
		/// 中优先级
		/// </summary>
		Medium = 1,

		/// <summary>
		/// 高优先级（最后回收）
		/// </summary>
		High = 2
	}
}
