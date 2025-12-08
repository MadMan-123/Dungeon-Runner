using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class RoomManager : NetworkBehaviour
{

    public Dictionary<string, List<Room>> rooms = new();
    public Room MainHub;

    [Header("Room Generation Settings")]
    [SerializeField] private int numberOfRooms = 10;
    [SerializeField] private bool generateOnSpawn = true;

    // Track spawned rooms
    private List<Room> spawnedRooms = new();
    private Room lastSpawnedRoom;
    public static RoomManager instance;

    private void Start()
    {
        if (!instance)
            instance = this;
        else
            Destroy(gameObject);
        
        // Organize room prefabs by type
        Dictionary<string, List<Room>> lists = new();

        foreach (var kv in PoolManager.Instance.pools)
        {
            string key = kv.Key;

            if (!Enum.TryParse<Room.Type>(key, out _))
                continue;

            var pool = kv.Value;
            var list = new List<Room>();

            // Fill list with ALL pooled objects
            foreach (var obj in pool.data.GetAllObjects())
            {
                var room = obj.GetComponent<Room>();
                if (room != null)
                    list.Add(room);
            }

            rooms[key] = list;

        }
        // Convert lists to dictionary
        foreach (var kv in lists)
        {
            rooms[kv.Key] = kv.Value;
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Only server generates rooms
        if (IsServer && generateOnSpawn)
        {
            StartCoroutine(Delayed());
        }
    }

    IEnumerator Delayed()
    {
        // Wait until PoolManager has registered rooms
        while (!PoolManager.Instance.RoomsRegistered)
            yield return null;

        yield return new WaitForSeconds(0.1f); 
        GenerateRoomChain();
    }

    /// Generates a chain of rooms connected via anchor points
    public void GenerateRoomChain()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can generate rooms!");
            return;
        }

        // Clear any existing rooms (but not MainHub)
        ClearRooms();

        // Start from the already-spawned MainHub
        if (MainHub == null)
        {
            Debug.LogError("MainHub is not assigned! Cannot generate dungeon.");
            return;
        }

        lastSpawnedRoom = MainHub;
        Debug.Log($"Starting dungeon generation from MainHub at {MainHub.transform.position}");

        // Generate the chain
        for (int i = 0; i < numberOfRooms; i++)
        {
            Room.Type roomType = DetermineNextRoomType(i);
            Room nextRoom = GetRandomRoomOfType(roomType);

            if (nextRoom != null)
            {
                SpawnAndConnectRoom(nextRoom);
            }
        }

        Debug.Log($"Generated room chain with {spawnedRooms.Count} rooms connected to MainHub");
    }

    /// Spawns a room from the pool and connects it to the last room
    private void SpawnAndConnectRoom(Room roomPrefab)
    {
        if (lastSpawnedRoom == null)
        {
            Debug.LogError("No previous room to connect to!");
            return;
        }

        // Calculate position and rotation to align anchors
        Vector3 position;
        Quaternion rotation;
        CalculateConnectionTransform(lastSpawnedRoom, roomPrefab, out position, out rotation);

        // Spawn the room
        Room newRoom = SpawnRoom(roomPrefab, position, rotation);
        
        if (newRoom != null)
        {
            lastSpawnedRoom = newRoom;
        }
    }

    /// Spawns a room from the pool manager
    private Room SpawnRoom(Room roomPrefab, Vector3 position, Quaternion rotation)
    {
        // Get pool key based on room type
        string poolKey = roomPrefab.type.ToString();
        var pool = PoolManager.Instance?.GetRandomRoomPool(poolKey);

        if (pool == null)
        {
            Debug.LogError($"Room pool not found for key: {poolKey}. Spawning directly.");
            
            var instance = Instantiate(roomPrefab.gameObject, position, rotation);
            var netObj = instance.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
            var room = instance.GetComponent<Room>();
            spawnedRooms.Add(room);
            return room;
        }

        // Get from pool
        var pooledObj = pool.data.GetObject();
        
        if (pooledObj != null)
        {
            pooledObj.transform.position = position;
            pooledObj.transform.rotation = rotation;
            
            var room = pooledObj.GetComponent<Room>();
            spawnedRooms.Add(room);
            
            Debug.Log($"Spawned room from pool: {poolKey} at {position}");
            return room;
        }
        
        Debug.LogError($"Failed to get room from pool: {poolKey}");
        return null;
    }

    /// Calculates the position and rotation needed to connect newRoom to previousRoom
    private void CalculateConnectionTransform(
        Room previousRoom,
        Room newRoomPrefab,
        out Vector3 position,
        out Quaternion rotation)
    {
        var prevEnd = previousRoom.AnchorEnd;
        var newStart = newRoomPrefab.AnchorStart;

        //Previous must always have an end anchor to connect FROM
        if (prevEnd == null)
        {
            Debug.LogWarning($"Previous room '{previousRoom.name}' has no AnchorEnd. Fallback.");
            position = previousRoom.transform.position + Vector3.forward * 10f;
            rotation = previousRoom.transform.rotation;
            return;
        }

        //New room may not have a start anchor, so fallback to pivot
        if (newStart == null)
        {
            Debug.LogWarning($"New room '{newRoomPrefab.name}' has no AnchorStart. Using room pivot instead.");

            // Align pivot to previous anchor End
            position = prevEnd.position;
            rotation = prevEnd.rotation;
            return;
        }

        Vector3 endPos = prevEnd.position;
        Quaternion endRot = prevEnd.rotation;

        Vector3 startLocalPos = newStart.localPosition;
        Quaternion startLocalRot = newStart.localRotation;

        // Rotate new room so its Start anchor aligns with previous End anchor forward direction
        rotation = endRot * Quaternion.Inverse(startLocalRot);

        // Move new room so its Start matches previous End in world space
        position = endPos - (rotation * startLocalPos);
    }


    /// Determines what type of room should come next in the chain
    private Room.Type DetermineNextRoomType(int index)
    {
        // Example logic: alternate between corridors and rooms
        if (index % 2 == 0)
        {
            return Room.Type.Corridor;
        }
        else
        {
            return Room.Type.Room;
        }
    }

    /// Gets a random room prefab of the specified type
    private Room GetRandomRoomOfType(Room.Type type)
    {
        var key = type.ToString();
        var pools = PoolManager.Instance.GetRoomPools(key);

        if (pools == null || pools.Count == 0)
            return null;

        // pick a random pool
        var pool = pools[UnityEngine.Random.Range(0, pools.Count)];

        // pick a random object from the pool
        var objects = pool.data.GetAllObjects();

        if (objects.Count == 0)
            return null;

        var obj = objects[UnityEngine.Random.Range(0, objects.Count)];
        return obj.GetComponent<Room>();
    }




    /// Clears all spawned rooms
    public void ClearRooms()
    {
        if (!IsServer) return;

        foreach (var room in spawnedRooms)
        {
            if (room != null)
            {
                string poolKey = room.type.ToString();
                var pool = PoolManager.Instance?.GetPool(poolKey);
                
                if (pool != null)
                {
                    pool.data.ReturnObject(room.NetworkObject);
                }
                else if (room.GetComponent<NetworkObject>() != null)
                {
                    room.GetComponent<NetworkObject>().Despawn();
                    Destroy(room.gameObject);
                }
            }
        }

        spawnedRooms.Clear();
        lastSpawnedRoom = null;
    }

  

    private void OnDestroy()
    {
        ClearRooms();
    }
}