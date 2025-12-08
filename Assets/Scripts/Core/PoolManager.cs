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
    public Dictionary<string,Pool> pools = new();
    
    public Dictionary<string, List<Pool>> roomPools = new();
    [SerializeField] private NetworkObject fireballPrefab;
    [SerializeField] private NetworkObject aiAgentPrefab;

    [InspectorLabel("Room Prefabs should be placed relative to the position in the Room.Type enum")]

    public List<NetworkObject> corridors;
    public List<NetworkObject> rooms;
    private List<List<NetworkObject>> roomPrefabs;
 
    private void Awake() 
    {
        Instance = this;
        roomPrefabs = new List<List<NetworkObject>>
        {
            null,
            corridors,  
            rooms,      
        }; 
    }
    
    public bool RoomsRegistered { get; private set; } = false;

    
    private void RegisterAllRoomPools()
    {
        roomPools.Clear();

        List<List<NetworkObject>> allLists = new() { corridors, rooms }; // optional: include MainHub if needed

        foreach (var list in allLists)
        {
            if (list == null) continue;

            foreach (var prefab in list)
            {
                if (prefab == null) continue;

                var roomComp = prefab.GetComponent<Room>();
                if (roomComp == null) continue;

                string typeName = roomComp.type.ToString();

                if (!roomPools.ContainsKey(typeName))
                    roomPools[typeName] = new List<Pool>();

                RegisterOneRoomPrefab(typeName, prefab);
            }
        }

        RoomsRegistered = true;
    }


    private void RegisterOneRoomPrefab(string key, NetworkObject prefab, int size = 20)
    {
        var pool = new Pool();

        var parentGO = new GameObject($"[{key}-Pool-{prefab.name}]");
        var parentNet = parentGO.AddComponent<NetworkObject>();
        parentNet.Spawn();
        parentGO.transform.SetParent(transform, false);

        pool.parent = parentGO;
        pool.data = new Core.NetworkGameObjectPool(prefab, size, parentGO.transform);

        roomPools[key].Add(pool);
    }

    public List<Pool> GetRoomPools(string key)
    {
        return roomPools.TryGetValue(key, out var list) ? list : null;
    }

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
            RegisterPool(nameof(Projectile.ProjectileType.FireBall), fireballPrefab);
            Debug.Log($"Added {nameof(Projectile.ProjectileType.FireBall)}");
            RegisterPool(nameof(Agent),aiAgentPrefab);
            RegisterAllRoomPools(); 
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