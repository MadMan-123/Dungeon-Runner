using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PoolManager : NetworkBehaviour
{
    public static PoolManager Instance;
    private Dictionary<string, Core.NetworkGameObjectPool> pools = new();
    
    [SerializeField] private NetworkObject fireballPrefab;
     
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
        }
        base.OnNetworkSpawn();
    }

    public void RegisterPool(string key, NetworkObject prefab, int size = 100)
    {
        if (!IsServer) return;
        
        Debug.Log($"RegisterPool called: {key}");
        
        // Create a child NetworkObject to act as pool container
        GameObject parent = new GameObject($"[{key}Pool]");
        NetworkObject parentNetObj = parent.AddComponent<NetworkObject>();
        parentNetObj.Spawn();
        parent.transform.SetParent(transform); // Parent to PoolManager
        
        pools[key] = new Core.NetworkGameObjectPool(prefab, size, parent.transform);
    }

    public Core.NetworkGameObjectPool GetPool(string key)
    {
        return pools.ContainsKey(key) ? pools[key] : null;
    }
}