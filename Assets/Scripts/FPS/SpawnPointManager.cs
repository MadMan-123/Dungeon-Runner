using System;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;


public class SpawnPointManager : NetworkBehaviour
{

    public static SpawnPointManager Instance;

    [SerializeField] Transform[] spawnPoints;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }


    public void SpawnPlayerIn(GameObject player)
    {
        
        
        //get random point
        int randomPoint = Random.Range(0, spawnPoints.Length);

        var point = spawnPoints[randomPoint];
        player.transform.position = point.position;
        player.transform.rotation = point.rotation;
    }

    private void OnDrawGizmos()
    {
        //draw each transform in spawnPoints
        if (spawnPoints is { Length: > 0 })
        {
            for (var i = 0; i < spawnPoints.Length; i++)
            {
                var point = spawnPoints[i];
                if (point == null) continue;
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(point.position, 0.5f);
                Gizmos.color = Color.white;
                Gizmos.DrawLine(point.position, point.position + point.forward * 2);
            }
        }
    }
}