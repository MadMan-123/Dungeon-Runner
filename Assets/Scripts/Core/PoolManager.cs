using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
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

    public Dictionary<string, Pool> pools = new();               
    public Dictionary<string, List<Pool>> roomPools = new();     

    [SerializeField] private NetworkObject fireballPrefab;
    [SerializeField] private NetworkObject arrowPrefab;
    [SerializeField] private NetworkObject aiAgentPrefab;

    // Indexed by enum order: 1 = corridors, 2 = rooms
    public List<NetworkObject> corridors;
    public List<NetworkObject> rooms;

    private List<List<NetworkObject>> roomPrefabs;

    private void Awake()
    {
        // Only one manager should exist
        Instance = this;

        // Build lookup table so room type enum can index into these lists
        roomPrefabs = new List<List<NetworkObject>>
        {
            null,        
            corridors,   
            rooms,       
        };
    }

    public bool RoomsRegistered { get; private set; } = false;

    // Registers all room types and creates pools for each prefab
    private void RegisterAllRoomPools()
    {
        roomPools.Clear();

        List<List<NetworkObject>> allLists = new() { corridors, rooms };

        foreach (var list in allLists)
        {
            if (list == null) continue;

            foreach (var prefab in list)
            {
                if (prefab == null) continue;

                var roomComp = prefab.GetComponent<Room>();
                if (roomComp == null) continue; // Not a valid room

                string typeName = roomComp.type.ToString();

                if (!roomPools.ContainsKey(typeName))
                    roomPools[typeName] = new List<Pool>();

                RegisterOneRoomPrefab(typeName, prefab);
            }
        }

        RoomsRegistered = true;
    }

    // Creates the pool for a single room prefab
    private void RegisterOneRoomPrefab(string key, NetworkObject prefab, int size = 20)
    {
        var pool = new Pool();

        // Parent object so the pool is organized in hierarchy
        var parentGO = new GameObject($"[{key}-Pool-{prefab.name}]");
        var parentNet = parentGO.AddComponent<NetworkObject>();
        parentNet.Spawn();
        parentGO.transform.SetParent(transform, false);

        pool.parent = parentGO;
        pool.data = new Core.NetworkGameObjectPool(prefab, size, parentGO.transform);

        roomPools[key].Add(pool);
    }

    // Returns all pools for a given room type
    public List<Pool> GetRoomPools(string key)
    {
        return roomPools.TryGetValue(key, out var list) ? list : null;
    }

    // Returns a random pool for a room type
    public Pool GetRandomRoomPool(string key)
    {
        if (roomPools.TryGetValue(key, out var list) && list.Count > 0)
            return list[UnityEngine.Random.Range(0, list.Count)];

        return null;
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            // Register projectile + AI pools
            RegisterPool(nameof(Projectile.ProjectileType.FireBall), fireballPrefab);
            RegisterPool(nameof(Agent), aiAgentPrefab);
            RegisterPool(nameof(Projectile.ProjectileType.Arrow), arrowPrefab);

            // Register all room pools
            RegisterAllRoomPools();
        }

        base.OnNetworkSpawn();
    }

    // Registers a general-purpose pool 
    public void RegisterPool(string key, NetworkObject prefab, int size = 100)
    {
        if (!IsServer) return;

        if (!pools.TryGetValue(key, out var pool))
        {
            pool = new();
            pools.Add(key, pool);
        }

        // Prevent double-registering
        if (pool.parent != null)
        {
            Debug.LogWarning($"Pool '{key}' already registered.");
            return;
        }

        // Create parent object so the pool stays organized
        var parentGO = new GameObject($"[{key}Pool]");
        var parentNet = parentGO.AddComponent<NetworkObject>();
        parentNet.Spawn();
        parentGO.transform.SetParent(transform, false);

        pool.parent = parentGO;
        pool.data = new Core.NetworkGameObjectPool(prefab, size, parentGO.transform);

        pools[key] = pool;
    }

    // Fetch an already-registered pool
    public Pool GetPool(string key)
    {
        return pools.ContainsKey(key) ? pools[key] : null;
    }
}
