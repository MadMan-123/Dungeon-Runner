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

        StartCoroutine(delay());
        IEnumerator delay()
        {
            yield return new WaitForSeconds(0.5f);
            SpawnIn(4);
        }
    }



    public void SpawnIn(int amount)
    {
        StartCoroutine(delay());
        IEnumerator delay()
        {
            yield return new WaitForSeconds(1f);
            SpawnInAgentsServerRPC(amount);
        } 
    }

    [ServerRpc]
    private void SpawnInAgentsServerRPC(int amount)
    {
           
        const string key = nameof(Agent);
        var pool = poolManager.GetPool(key);
        if (SpawnPointManager.Instance == null)
        {
            Debug.LogError("SpawnPointManager.Instance is null");
            return;
        }

        // Ensure points are current in case rooms/spawn markers were generated after Awake.
        SpawnPointManager.Instance.RefreshPoints();
        var map = SpawnPointManager.Instance.pointMap;
        Debug.Log($"[AgentManager] Spawn map counts: " + string.Join(", ", map.Select(kv => $"{kv.Key}={kv.Value.Length}")));

        spawnPoints = SpawnPointManager.Instance.GetPoints("Enemy");
        if (pool == null)
        {
            Debug.LogError("Pool is null");
            return;
        }

        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogError("Spawn points are null or empty. Call SpawnPointManager.RefreshPoints after room generation.");
            return;
        }
        int maxSpawns = Mathf.Min(amount, spawnPoints.Length);
 
        
        //try not to spawn two enemies on 1 point
        HashSet<int> used = new();
        int tried = 0;
        for (int i = 0; i < maxSpawns; i++)
        {
          
            
            // Find a free spawn point
            int attempts = 0;
            int index;
            do
            {
                index = Random.Range(0, spawnPoints.Length );
                attempts++;

                if (attempts > spawnPoints.Length + 1)
                {
                    Debug.LogWarning("No free spawn points left");
                    return;
                }

            } while (used.Contains(index));
            
            used.Add(index);
            var obj = pool.data.GetObject();
            if (obj == null) 
                return;
            var point = spawnPoints[index];
            if (point == null)
            {
                Debug.LogError("No point was valid`");
            }
            obj.transform.SetPositionAndRotation(point.position, point.rotation);

            if (!obj.IsSpawned)
                obj.Spawn(true);
            obj.transform.SetParent(pool.parent.transform, true);
 
            if (obj.TryGetComponent(out Agent agent))
            {
                agent.Init(point.position + Vector3.up * 2f, Quaternion.Euler(-90,0,0), pool.data);
            }        
        }
    }
}
