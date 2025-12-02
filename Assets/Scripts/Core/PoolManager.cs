using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PoolManager : NetworkBehaviour
{
    public static PoolManager Instance;
    private Dictionary<string, Core.NetworkGameObjectPool> pools = new();
    
    [SerializeField] private NetworkObject bulletPrefab;
    
    private void Awake() 
    {
        Instance = this;
        Debug.Log("PoolManager Awake");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            Debug.Log("PoolManager creating bullet pool");
            // Pass THIS NetworkObject's transform as parent (since PoolManager is a NetworkBehaviour)
            RegisterPool("Bullets", bulletPrefab, 100);
        }
        base.OnNetworkSpawn();
    }

    public void RegisterPool(string key, NetworkObject prefab, int size)
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