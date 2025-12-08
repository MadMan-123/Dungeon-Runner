using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class PoolManager : NetworkBehaviour
{
    public class Pool
    {
        public Core.NetworkGameObjectPool data;
        public GameObject parent;
    }
    public static PoolManager Instance;
    private Dictionary<string,Pool> pools = new();
    
    [SerializeField] private NetworkObject fireballPrefab;
    [SerializeField] private NetworkObject aiAgentPrefab;
     
    private void Awake() 
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            RegisterPool(nameof(Projectile.ProjectileType.FireBall), fireballPrefab);
            Debug.Log($"Added {nameof(Projectile.ProjectileType.FireBall)}");
            RegisterPool(nameof(Agent),aiAgentPrefab);
        }
        base.OnNetworkSpawn();
    }

    public void RegisterPool(string key, NetworkObject prefab, int size = 100)
    {
        if (!IsServer) return;
        
        Debug.Log($"RegisterPool called: {key}");
        if (!pools.TryGetValue(key, out var pool))
        {
            pool = new ();   
            pools.Add(key, pool);
        } 
        if (pool.parent != null)
        {
            Debug.LogWarning($"Pool '{key}' already registered.");
            return;
        } 
        
        var parentGO = new GameObject($"[{key}Pool]");
        var parentNet = parentGO.AddComponent<NetworkObject>();
        parentNet.Spawn();
        parentGO.transform.SetParent(transform, false);
        pool.parent = parentGO;
        pool.data = new Core.NetworkGameObjectPool(prefab, size, parentGO.transform);
        pools[key] = pool;

    }

    public Pool GetPool(string key)
    {
        return pools.ContainsKey(key) ? pools[key] : null;
    }
}