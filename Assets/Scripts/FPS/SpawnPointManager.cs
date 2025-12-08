using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;


public class SpawnPointManager : NetworkBehaviour
{

    [Serializable]
    public struct Point
    {
        public Transform transform;
        public string tag;
    }
    
    public static SpawnPointManager Instance;

    [SerializeField] Point[] spawnPoints;
    public Dictionary<string, Transform[]> pointMap = new();
    
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

    const int MAX_SIZE = 100;
    private void Start()
    {
        Dictionary<string, List<Transform>> lists = new();

        foreach (var data in spawnPoints)
        {
            if (!lists.TryGetValue(data.tag, out var list))
            {
                list = new List<Transform>();
                lists[data.tag] = list;
            }

            list.Add(data.transform);
        }
        // Convert lists to arrays
        foreach (var kv in lists)
        {
            pointMap[kv.Key] = kv.Value.ToArray();
        } 
    }

    public Transform[] GetPoints(string tag)
    {
        if (!pointMap.TryGetValue(tag, out var transforms))
        {
            Debug.LogError($"Key {tag} does not exist in the map");
            return Array.Empty<Transform>();
        }

        return transforms;
    }

    

    
    public void SpawnPlayerIn(GameObject player)
    {
       
        //get random point

        //get a random point from the player tag
        if (!pointMap.TryGetValue("Player", out var transforms))
        {
            Debug.LogError("Player tag not found");
            return;
        }
        
        int randomPoint = Random.Range(0, transforms.Length);
        var point = transforms[randomPoint];
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
                var point= spawnPoints[i];
                var pointTag = point.tag;
                if (point.transform == null) continue;

                var colour = pointTag switch
                {
                    "Player" => Color.green,
                    "Enemy" => Color.darkRed,
                    _ => Color.purple
                };
                
                Gizmos.color = colour;
                Gizmos.DrawSphere(point.transform.position, 0.5f);
                Gizmos.color = Color.white;
                Gizmos.DrawLine(point.transform.position, point.transform.position + point.transform.forward * 2);
            }
        }
    }
}