using BEWGame.Pool;
using UnityEngine;
using System.Collections;
using BEWGame; // 添加BEWGame命名空间引用

/// <summary>
/// 测试对象池对象
/// 提供完整的IPoolObject接口实现和测试功能
/// </summary>
public class TestUnityPoolObject : MonoBehaviour, BEWGame.Pool.IPoolObject
{
	[SerializeField] private ParticleSystem _particleEffect;
	[SerializeField] private float _lifeTime = 3f;
	
	private Coroutine _lifetimeCoroutine;
	private bool _isInitialized = false;
	
	// IPoolObject接口实现
	public string PoolType { get; set; }
	public int ReferenceCount { get; set; }
	public bool IsActive => this.gameObject.activeSelf;
	public float CreationTime { get; set; }
	public float LastActiveTime { get; set; }
	
	// 新增接口实现
	public bool IsRecyclable { get; set; } = true;
	public float LastUsedTime { get; set; }
	public float Lifetime { get; set; }
	public int Priority { get; set; }

	private void Awake()
	{
		// 确保有粒子系统组件
		if (_particleEffect == null)
		{
			_particleEffect = GetComponentInChildren<ParticleSystem>();
		}
	}

	/// <summary>
	/// 对象被创建时调用
	/// </summary>
	public void OnCreate(string poolType)
	{
		PoolType = poolType;
		CreationTime = Time.time;
		LastActiveTime = Time.time;
		LastUsedTime = Time.time;
		Lifetime = _lifeTime;
		ReferenceCount = 0;
		Priority = 0;
		IsRecyclable = true;
		_isInitialized = true;
		
		Debug.Log($"[TestPoolObject] {gameObject.name} 已创建，池类型: {poolType}");
	}

	/// <summary>
	/// 对象被取出时调用
	/// </summary>
	public void OnSpawn()
	{
		if (!_isInitialized)
		{
			Debug.LogWarning($"[TestPoolObject] {gameObject.name} 在Spawn前未初始化");
			// 使用OnCreate替代Initialize
			OnCreate("Unknown");
		}
		
		gameObject.SetActive(true);
		LastActiveTime = Time.time;
		LastUsedTime = Time.time;
		
		// 播放粒子效果
		if (_particleEffect != null)
		{
			_particleEffect.Play();
		}
		
		// 启动生命周期协程
		if (_lifeTime > 0)
		{
			_lifetimeCoroutine = StartCoroutine(LifetimeRoutine());
		}
		
		Debug.Log($"[TestPoolObject] {gameObject.name} 已激活，将在 {_lifeTime} 秒后自动回收");
	}

	/// <summary>
	/// 对象被回收时调用
	/// </summary>
	public void OnDespawn()
	{
		// 停止所有协程
		if (_lifetimeCoroutine != null)
		{
			StopCoroutine(_lifetimeCoroutine);
			_lifetimeCoroutine = null;
		}
		
		// 停止粒子效果
		if (_particleEffect != null)
		{
			_particleEffect.Stop();
		}
		
		// 重置状态
		OnReset();
		
		// 禁用游戏对象
		gameObject.SetActive(false);
		
		Debug.Log($"[TestPoolObject] {gameObject.name} 已回收到对象池");
	}

	/// <summary>
	/// 对象被销毁时调用
	/// </summary>
	public void OnDestroy()
	{
		// 清理资源
		Debug.Log($"[TestPoolObject] {gameObject.name} 已销毁");
	}

	/// <summary>
	/// 重置对象状态
	/// </summary>
	public void OnReset()
	{
		// 重置变换
		transform.position = Vector3.zero;
		transform.rotation = Quaternion.identity;
		transform.localScale = Vector3.one;
		
		// 重置其他组件状态
		var renderers = GetComponentsInChildren<Renderer>();
		foreach (var renderer in renderers)
		{
			renderer.enabled = true;
		}
	}

	/// <summary>
	/// 增加引用计数
	/// </summary>
	public void AddReference()
	{
		ReferenceCount++;
		Debug.Log($"[TestPoolObject] {gameObject.name} 引用计数增加到 {ReferenceCount}");
	}

	/// <summary>
	/// 减少引用计数
	/// </summary>
	public bool RemoveReference()
	{
		ReferenceCount = Mathf.Max(0, ReferenceCount - 1);
		Debug.Log($"[TestPoolObject] {gameObject.name} 引用计数减少到 {ReferenceCount}");
		return ReferenceCount == 0;
	}
	
	/// <summary>
	/// 生命周期协程
	/// </summary>
	private IEnumerator LifetimeRoutine()
	{
		yield return new WaitForSeconds(_lifeTime);
		
		// 自动回收到对象池
		if (gameObject.activeInHierarchy)
		{
			Debug.Log($"[TestPoolObject] {gameObject.name} 生命周期结束，请求回收");
			
			// 使用GameEntry.poolUnityMgr替代FindObjectOfType<PoolMgr>()
			if (GameEntry.poolUnityMgr != null)
			{
				// 检查引用计数，如果引用计数为0或者调用RemoveReference后为0，则回收
				if (ReferenceCount == 0 || RemoveReference())
				{
					GameEntry.poolUnityMgr.Despawn(this as BEWGame.Pool.IPoolObject);
				}
				else
				{
					Debug.Log($"[TestPoolObject] {gameObject.name} 引用计数不为0，暂不回收");
				}
			}
			else
			{
				Debug.LogWarning("[TestPoolObject] GameEntry.poolUnityMgr为空，无法自动回收");
				gameObject.SetActive(false);
			}
		}
	}
	
	/// <summary>
	/// 测试方法：随机移动
	/// </summary>
	public void RandomMove()
	{
		Vector3 randomPos = new Vector3(
			Random.Range(-5f, 5f),
			Random.Range(-5f, 5f),
			Random.Range(-5f, 5f)
		);
		
		transform.position = randomPos;
	}
	
	/// <summary>
	/// 测试方法：改变颜色
	/// </summary>
	public void ChangeColor(Color color)
	{
		var renderers = GetComponentsInChildren<Renderer>();
		foreach (var renderer in renderers)
		{
			if (renderer.material != null)
			{
				renderer.material.color = color;
			}
		}
	}
}
