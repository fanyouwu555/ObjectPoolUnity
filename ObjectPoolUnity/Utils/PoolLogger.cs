/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：PoolLogger
 * 
 * 创建者：Claude AI
 * 创建时间：3/13/2024
 * 描述：对象池系统日志工具类，使用条件编译控制日志输出
 *----------------------------------------------------------------*/

using System;
using UnityGameFramework.Runtime;

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池系统日志工具类
	/// 使用条件编译控制日志输出
	/// </summary>
	public static class PoolLogger
	{
		// 日志级别
		public enum LogLevel
		{
			Debug = 0,
			Info = 1,
			Warning = 2,
			Error = 3,
			None = 4
		}

		// 当前日志级别，可以根据环境动态设置
		private static LogLevel _currentLogLevel = LogLevel.Info;

		// 是否启用详细日志
		private static bool _enableVerboseLogging = false;

		// 是否启用性能日志
		private static bool _enablePerformanceLogging = false;

		// 是否启用调试日志
		private static bool _enableDebugLogging =
#if UNITY_EDITOR || DEVELOPMENT_BUILD || POOL_DEBUG
			true;
#else
            false;
#endif

		/// <summary>
		/// 设置日志级别
		/// </summary>
		/// <param name="level">日志级别</param>
		public static void SetLogLevel(LogLevel level)
		{
			_currentLogLevel = level;
		}

		/// <summary>
		/// 启用或禁用详细日志
		/// </summary>
		/// <param name="enable">是否启用</param>
		public static void EnableVerboseLogging(bool enable)
		{
			_enableVerboseLogging = enable;
		}

		/// <summary>
		/// 启用或禁用性能日志
		/// </summary>
		/// <param name="enable">是否启用</param>
		public static void EnablePerformanceLogging(bool enable)
		{
			_enablePerformanceLogging = enable;
		}

		/// <summary>
		/// 输出调试日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Debug(string message, UnityEngine.Object context = null)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || POOL_DEBUG
			if (_enableDebugLogging && _currentLogLevel <= LogLevel.Debug)
			{
				Log.Debug($"[Pool] {message}", context);
			}
#endif
		}

		/// <summary>
		/// 输出信息日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Info(string message, UnityEngine.Object context = null)
		{
			if (_currentLogLevel <= LogLevel.Info)
			{
				Log.Info($"[Pool] {message}", context);
			}
		}

		/// <summary>
		/// 输出警告日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Warning(string message, UnityEngine.Object context = null)
		{
			if (_currentLogLevel <= LogLevel.Warning)
			{
				Log.Warning($"[Pool] {message}", context);
			}
		}

		/// <summary>
		/// 输出错误日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Error(string message, UnityEngine.Object context = null)
		{
			if (_currentLogLevel <= LogLevel.Error)
			{
				Log.Error($"[Pool] {message}", context);
			}
		}

		/// <summary>
		/// 输出异常日志
		/// </summary>
		/// <param name="exception">异常对象</param>
		/// <param name="context">上下文对象</param>
		public static void Exception(Exception exception, UnityEngine.Object context = null)
		{
			if (_currentLogLevel <= LogLevel.Error)
			{
				Log.Error($"[exception] {exception.Message}", context);
			}
		}

		/// <summary>
		/// 输出详细日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Verbose(string message, UnityEngine.Object context = null)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || POOL_DEBUG
			if (_enableVerboseLogging && _currentLogLevel <= LogLevel.Debug)
			{
				Log.Debug($"[Pool][Verbose] {message}", context);
			}
#endif
		}

		/// <summary>
		/// 输出性能日志
		/// </summary>
		/// <param name="message">日志消息</param>
		/// <param name="context">上下文对象</param>
		public static void Performance(string message, UnityEngine.Object context = null)
		{
#if UNITY_EDITOR || DEVELOPMENT_BUILD || POOL_PERFORMANCE
			if (_enablePerformanceLogging && _currentLogLevel <= LogLevel.Debug)
			{
				Log.Debug($"[Pool][Performance] {message}", context);
			}
#endif
		}
	}
}