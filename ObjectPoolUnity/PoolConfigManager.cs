/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：PoolConfigManager
 * 
 * 创建者：Claude AI
 * 创建时间：3/12/2024 5:35:35 PM
 * 描述：对象池配置管理器，负责管理对象池配置
 *----------------------------------------------------------------*/

using System.Collections.Generic;
using UnityEngine;
//#if UNITY_EDITOR
//using UnityEditor;
//#endif

namespace BEWGame.Pool
{
	/// <summary>
	/// 对象池配置管理器
	/// 负责管理对象池配置
	/// </summary>
	public class PoolConfigManager : MonoBehaviour, IPoolConfigManager
	{
		// 配置字典
		private Dictionary<string, PoolPrefabConfig> _configs = new Dictionary<string, PoolPrefabConfig>();

		// 配置资源
		private ObjectPoolConfig _configAsset;

		// 是否已初始化
		private bool _initialized = false;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="configAsset">配置资源</param>
		public void init(ObjectPoolConfig configAsset)
		{
			_configAsset = configAsset;
			Initialize();
		}

		/// <summary>
		/// 初始化
		/// </summary>
		private void Initialize()
		{
			if (_initialized)
			{
				return;
			}

			if (_configAsset == null)
			{
				PoolLogger.Warning("[PoolConfigManager] 配置资源为空，使用默认配置");
				return;
			}

			// 加载配置
			foreach (var config in _configAsset.prefabConfigs)
			{
				if (config == null || string.IsNullOrEmpty(config.poolTp))
				{
					PoolLogger.Warning("[PoolConfigManager] 发现无效配置，已跳过");
					continue;
				}

				// 添加配置
				_configs[config.poolTp] = config.Clone();
			}

			PoolLogger.Info($"[PoolConfigManager] 已加载 {_configs.Count} 个对象池配置");
			_initialized = true;
		}

		/// <summary>
		/// 获取对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>对象池配置</returns>
		public PoolPrefabConfig GetConfig(string poolType)
		{
			if (string.IsNullOrEmpty(poolType))
			{
				PoolLogger.Warning("[PoolConfigManager] 获取配置失败：池类型为空");
				return null;
			}

			if (_configs.TryGetValue(poolType, out var config))
			{
				return config;
			}

			PoolLogger.Warning($"[PoolConfigManager] 未找到对象池 {poolType} 的配置");
			return null;
		}

		/// <summary>
		/// 添加对象池配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		public void AddConfig(PoolPrefabConfig config)
		{
			if (config == null || string.IsNullOrEmpty(config.poolTp))
			{
				PoolLogger.Error("[PoolConfigManager] 添加配置失败：配置无效");
				return;
			}

			if (_configs.ContainsKey(config.poolTp))
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的配置已存在，将被覆盖");
			}

			_configs[config.poolTp] = config.Clone();
			PoolLogger.Info($"[PoolConfigManager] 已添加对象池 {config.poolTp} 的配置");

			// 更新配置资源
			UpdateConfigAsset();
		}

		/// <summary>
		/// 更新对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <param name="config">新配置</param>
		public void UpdateConfig(string poolType, PoolPrefabConfig config)
		{
			if (string.IsNullOrEmpty(poolType))
			{
				PoolLogger.Error("[PoolConfigManager] 更新配置失败：池类型为空");
				return;
			}

			if (config == null)
			{
				PoolLogger.Error("[PoolConfigManager] 更新配置失败：配置为空");
				return;
			}

			if (!_configs.ContainsKey(poolType))
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {poolType} 的配置不存在，将添加新配置");
			}

			// 确保配置ID一致
			config.poolTp = poolType;
			_configs[poolType] = config.Clone();
			PoolLogger.Info($"[PoolConfigManager] 已更新对象池 {poolType} 的配置");

			// 更新配置资源
			UpdateConfigAsset();
		}

		/// <summary>
		/// 移除对象池配置
		/// </summary>
		/// <param name="poolType">对象池类型</param>
		/// <returns>是否成功移除</returns>
		public bool RemoveConfig(string poolType)
		{
			if (string.IsNullOrEmpty(poolType))
			{
				PoolLogger.Error("[PoolConfigManager] 移除配置失败：池类型为空");
				return false;
			}

			if (_configs.Remove(poolType))
			{
				PoolLogger.Info($"[PoolConfigManager] 已移除对象池 {poolType} 的配置");

				// 更新配置资源
				UpdateConfigAsset();
				return true;
			}

			PoolLogger.Warning($"[PoolConfigManager] 对象池 {poolType} 的配置不存在，无法移除");
			return false;
		}

		/// <summary>
		/// 获取所有对象池配置
		/// </summary>
		/// <returns>所有对象池配置</returns>
		public List<PoolPrefabConfig> GetAllConfigs()
		{
			List<PoolPrefabConfig> result = new List<PoolPrefabConfig>();

			foreach (var config in _configs.Values)
			{
				result.Add(config.Clone());
			}

			return result;
		}

		/// <summary>
		/// 更新配置资源
		/// </summary>
		private void UpdateConfigAsset()
		{
			if (_configAsset == null)
			{
				PoolLogger.Warning("[PoolConfigManager] 配置资源为空，无法更新");
				return;
			}

			// 更新配置资源
			_configAsset.prefabConfigs.Clear();
			foreach (var config in _configs.Values)
			{
				_configAsset.prefabConfigs.Add(config.Clone());
			}

			//			// 标记为已修改
			//#if UNITY_EDITOR
			//			EditorUtility.SetDirty(_configAsset);
			//			AssetDatabase.SaveAssets();
			//			PoolLogger.Info("[PoolConfigManager] 已保存配置资源");
			//#endif
		}

		/// <summary>
		/// 重置配置
		/// </summary>
		public void ResetConfigs()
		{
			_configs.Clear();
			Initialize();
			PoolLogger.Info("[PoolConfigManager] 已重置所有配置");
		}

		/// <summary>
		/// 设置配置资源
		/// </summary>
		/// <param name="configAsset">配置资源</param>
		public void SetConfigAsset(ObjectPoolConfig configAsset)
		{
			_configAsset = configAsset;
			_initialized = false;
			Initialize();
			PoolLogger.Info("[PoolConfigManager] 已设置新的配置资源");
		}

		/// <summary>
		/// 验证配置
		/// </summary>
		/// <param name="config">要验证的配置</param>
		/// <returns>是否有效</returns>
		public bool ValidateConfig(PoolPrefabConfig config)
		{
			if (config == null)
			{
				PoolLogger.Error("[PoolConfigManager] 验证配置失败：配置为空");
				return false;
			}

			if (string.IsNullOrEmpty(config.poolTp))
			{
				PoolLogger.Error("[PoolConfigManager] 验证配置失败：池类型为空");
				return false;
			}

			if (config.prefab == null)
			{
				PoolLogger.Error($"[PoolConfigManager] 验证配置失败：对象池 {config.poolTp} 的预制体为空");
				return false;
			}

			if (config.initialCount < 0)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的初始数量小于0，已设置为0");
				config.initialCount = 0;
			}

			if (config.maxCount <= 0)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的最大数量小于等于0，已设置为10");
				config.maxCount = 10;
			}

			if (config.initialCount > config.maxCount)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的初始数量大于最大数量，已调整为最大数量");
				config.initialCount = config.maxCount;
			}

			if (config.minRetainCount < 0)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的最小保留数量小于0，已设置为0");
				config.minRetainCount = 0;
			}

			if (config.minRetainCount > config.maxCount)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的最小保留数量大于最大数量，已调整为最大数量");
				config.minRetainCount = config.maxCount;
			}

			if (config.autoExpandRatio <= 0f || config.autoExpandRatio > 1f)
			{
				PoolLogger.Warning($"[PoolConfigManager] 对象池 {config.poolTp} 的自动扩展比例无效，已设置为0.2");
				config.autoExpandRatio = 0.2f;
			}

			return true;
		}
	}
}
