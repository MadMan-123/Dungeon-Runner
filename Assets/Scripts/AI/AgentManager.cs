using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class AgentManager : NetworkBehaviour
{
    public static AgentManager Instance;
    public PoolManager poolManager;
    private NetworkGameObjectPool pool;
    private Transform[] spawnPoints;


    private void Start()
    {
        //Singleton
        if (!Instance)
        {
            Instance = this;
        }
        else
        {
            Destroy(Instance);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Spawn in agents
        StartCoroutine(delay());
        
        IEnumerator delay()
        {
            yield return new WaitForSeconds(5f);
            SpawnIn(100);
        }
    }


    
    public void SpawnIn(int amount)
    {
        SpawnInAgentsServerRPC(amount);
    }

    [ServerRpc]
private void SpawnInAgentsServerRPC(int amount)
{
    const string key = nameof(Agent);

    // Get pool
    var pool = poolManager.GetPool(key);
    if (pool == null)
    {
        Debug.LogError("Pool is null");
        return;
    }

    // Get spawn points
    if (SpawnPointManager.Instance == null)
    {
        Debug.LogError("SpawnPointManager.Instance is null");
        return;
    }

    SpawnPointManager.Instance.RefreshPoints();

    spawnPoints = SpawnPointManager.Instance.GetPoints("Enemy");
    if (spawnPoints == null || spawnPoints.Length == 0)
    {
        Debug.LogError("Enemy spawn points missing");
        return;
    }

    // Convert to list and shuffle (Fisher Yates)
    List<Transform> points = spawnPoints.ToList();
    for (int i = points.Count - 1; i > 0; i--)
    {
        int rand = Random.Range(0, i + 1);
        (points[i], points[rand]) = (points[rand], points[i]);
    }

    // Spawn up to number of available points
    int spawnCount = Mathf.Min(amount, points.Count);

    for (int i = 0; i < spawnCount; i++)
    {
        var point = points[i];
        if (point == null)
        {
            Debug.LogError("Null spawn point encountered");
            continue;
        }

        // Spawn from pool
        var obj = pool.data.GetObject();
        if (obj == null)
        {
            Debug.LogError("Pool returned null object");
            continue;
        }

        // Set position & rotation
        obj.transform.SetPositionAndRotation(point.position, point.rotation);

        // Network spawn
        if (!obj.IsSpawned)
            obj.Spawn(true);

        obj.transform.SetParent(pool.parent.transform, true);

        // Initialise agent
        if (obj.TryGetComponent(out Agent agent))
        {
            agent.Init(
                point.position + Vector3.up * 2f,
                Quaternion.Euler(-90, 0, 0),
                pool.data
            );
        }
    }

    Debug.Log($"[AgentManager] Spawned {spawnCount} enemies across {points.Count} valid points.");
}

}

