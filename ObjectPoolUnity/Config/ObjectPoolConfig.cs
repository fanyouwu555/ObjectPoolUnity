/*----------------------------------------------------------------
 * 版权所有 (c) 2024   保留所有权利。
 * 文件名：ObjectPoolConfig
 * 
 * 创建者：Frandys
 * 修改者：Claude AI
 * 创建时间：12/23/2024 8:33:50 PM
 * 描述：对象池配置类，用于配置对象池参数
 *----------------------------------------------------------------*/


namespace BEWGame.Pool
{
	using UnityEngine;
	using System.Collections.Generic;
	using System;

	/// <summary>
	/// 单个预制体的配置信息
	/// </summary>
	[Serializable]
	public class PoolPrefabConfig
	{
		/// <summary>
		/// 对象池的唯一标识符
		/// </summary>
		public string poolTp;
		
		/// <summary>
		/// 要放入对象池的预制体
		/// </summary>
		public GameObject prefab;
		
		/// <summary>
		/// 对象池初始预加载的数量
		/// </summary>
		public int initialCount;
		
		/// <summary>
		/// 对象池最大数量
		/// </summary>
		public int maxCount;
		
		/// <summary>
		/// 对象池最小保留数量
		/// </summary>
		public int minRetainCount;
		
		/// <summary>
		/// 是否允许自动扩展
		/// </summary>
		public bool allowAutoExpand = true;
		
		/// <summary>
		/// 自动扩展比例
		/// </summary>
		public float autoExpandRatio = 0.2f;

		/// <summary>
		/// 构造函数
		/// </summary>
		/// <param name="id">对象池ID</param>
		/// <param name="prefab">预制体</param>
		/// <param name="initialCount">初始数量</param>
		/// <param name="maxCount">最大数量</param>
		/// <param name="minRetainCount">最小保留数量</param>
		public PoolPrefabConfig(string id, GameObject prefab, int initialCount, int maxCount, int minRetainCount = 0)
		{
			this.poolTp = id;
			this.prefab = prefab;
			this.initialCount = initialCount;
			this.maxCount = maxCount;
			this.minRetainCount = minRetainCount;
			this.allowAutoExpand = true;
			this.autoExpandRatio = 0.2f;
		}
		
		/// <summary>
		/// 创建配置的副本
		/// </summary>
		/// <returns>配置副本</returns>
		public PoolPrefabConfig Clone()
		{
			return new PoolPrefabConfig(poolTp, prefab, initialCount, maxCount, minRetainCount)
			{
				allowAutoExpand = this.allowAutoExpand,
				autoExpandRatio = this.autoExpandRatio
			};
		}
	}

	/// <summary>
	/// 对象池配置类，用于配置多个对象池相关参数
	/// </summary>
	[CreateAssetMenu(fileName = "ObjectPoolConfig", menuName = "Custom Object Pool/Object Pool Config", order = 1)]
	public class ObjectPoolConfig : ScriptableObject
	{
		/// <summary>
		/// 存储多个预制体配置的列表
		/// </summary>
		public List<PoolPrefabConfig> prefabConfigs = new List<PoolPrefabConfig>();

		/// <summary>
		/// 用于在编辑器中快速查找配置的字典
		/// </summary>
		private Dictionary<string, PoolPrefabConfig> _configLookup;

		/// <summary>
		/// 初始化查找字典
		/// </summary>
		public void InitializeLookup()
		{
			_configLookup = new Dictionary<string, PoolPrefabConfig>();
			foreach (var config in prefabConfigs)
			{
				if (!string.IsNullOrEmpty(config.poolTp) && !_configLookup.ContainsKey(config.poolTp))
				{
					_configLookup[config.poolTp] = config;
				}
			}
		}

		/// <summary>
		/// 根据ID获取配置
		/// </summary>
		/// <param name="poolTp">对象池ID</param>
		/// <returns>对象池配置</returns>
		public PoolPrefabConfig GetConfigById(string poolTp)
		{
			if (_configLookup == null)
			{
				InitializeLookup();
			}

			if (_configLookup.TryGetValue(poolTp, out var config))
			{
				return config;
			}
			return null;
		}

		/// <summary>
		/// 添加新配置
		/// </summary>
		/// <param name="config">对象池配置</param>
		public void AddConfig(PoolPrefabConfig config)
		{
			if (config == null || string.IsNullOrEmpty(config.poolTp))
			{
				Debug.LogError("[ObjectPoolConfig] 无法添加配置，配置为空或ID为空");
				return;
			}
			
			if (_configLookup == null)
			{
				InitializeLookup();
			}

			// 检查是否已存在
			if (_configLookup.ContainsKey(config.poolTp))
			{
				// 更新现有配置
				for (int i = 0; i < prefabConfigs.Count; i++)
				{
					if (prefabConfigs[i].poolTp == config.poolTp)
					{
						prefabConfigs[i] = config;
						_configLookup[config.poolTp] = config;
						return;
					}
				}
			}
			
			// 添加新配置
			prefabConfigs.Add(config);
			_configLookup[config.poolTp] = config;
		}
		
		/// <summary>
		/// 移除配置
		/// </summary>
		/// <param name="poolTp">对象池ID</param>
		/// <returns>是否成功移除</returns>
		public bool RemoveConfig(string poolTp)
		{
			if (string.IsNullOrEmpty(poolTp))
			{
				return false;
			}
			
			if (_configLookup == null)
			{
				InitializeLookup();
			}
			
			if (_configLookup.ContainsKey(poolTp))
			{
				_configLookup.Remove(poolTp);
				
				for (int i = 0; i < prefabConfigs.Count; i++)
				{
					if (prefabConfigs[i].poolTp == poolTp)
					{
						prefabConfigs.RemoveAt(i);
						return true;
					}
				}
			}
			
			return false;
		}
		
		/// <summary>
		/// 获取所有配置
		/// </summary>
		/// <returns>所有配置的副本</returns>
		public List<PoolPrefabConfig> GetAllConfigs()
		{
			var result = new List<PoolPrefabConfig>();
			
			foreach (var config in prefabConfigs)
			{
				result.Add(config.Clone());
			}
			
			return result;
		}
		
		/// <summary>
		/// 清空所有配置
		/// </summary>
		public void ClearAllConfigs()
		{
			prefabConfigs.Clear();
			
			if (_configLookup != null)
			{
				_configLookup.Clear();
			}
		}
	}
}
