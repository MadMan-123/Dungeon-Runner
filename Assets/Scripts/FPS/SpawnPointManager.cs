using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
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

    [SerializeField] private Point[] spawnPoints = new Point[50];
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
        RefreshPoints();
    }

    public void GetAllSpawnPoints()
    {
        List<Point> allPoints = new();

        // Enemy points
        var enemyPoints = GameObject.FindGameObjectsWithTag("EnemySpawnPoint");
        foreach (var go in enemyPoints)
        {
            Point data;
            data.transform = go.transform;
            data.tag = "Enemy";
            allPoints.Add(data);
        }

        // Player points
        var playerPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
        foreach (var go in playerPoints)
        {
            Point data;
            data.transform = go.transform;
            data.tag = "Player";
            allPoints.Add(data);
        }

        spawnPoints = allPoints.ToArray();
    }

    private void BuildPointMap()
    {
        Dictionary<string, List<Transform>> lists = new();
        foreach (var data in spawnPoints)
        {
            if (data.transform == null) continue;

            if (!lists.TryGetValue(data.tag, out var list))
            {
                list = new List<Transform>();
                lists[data.tag] = list;
            }
            list.Add(data.transform);
        }

        pointMap.Clear();
        foreach (var kv in lists)
        {
            pointMap[kv.Key] = kv.Value.ToArray();
        }
    }

    public void RefreshPoints()
    {
        GetAllSpawnPoints();
        BuildPointMap();
        Debug.Log($"[SpawnPointManager] Refreshed spawn points. Counts: " +
                  string.Join(", ", pointMap.Select(kv => $"{kv.Key}={kv.Value.Length}")));
    }


    const int MAX_SIZE = 100;
    private void Start()
    {
        // Ensure map is ready even if Awake ran before runtime-generated rooms spawned.
        if (pointMap.Count == 0)
        {
            RefreshPoints();
        }
    }


    public Transform[] GetPoints(string tag)
    {
        if (!pointMap.TryGetValue(tag, out var transforms) || transforms.Length == 0)
        {
            Debug.LogError($"Key {tag} does not exist in the map or has zero entries");
            return Array.Empty<Transform>();
        }

        return transforms;
    }

    

    public void SpawnPlayerIn(GameObject player)
    {
        if (!pointMap.TryGetValue("Player", out var transforms))
        {
            Debug.LogError("Player tag not found");
            return;
        }

        int randomPoint = Random.Range(0, transforms.Length);
        var point = transforms[randomPoint];

        Vector3 spawnPos = point.position + Vector3.up * 2f; // initial offset

        // Raycast down to find ground
        if (Physics.Raycast(spawnPos, Vector3.down, out RaycastHit hit, 5f))
        {
            spawnPos.y = hit.point.y + 0.1f; // tiny buffer so CC doesn't clip
        }

        player.transform.position = spawnPos;
        player.transform.rotation = point.rotation;

        var controller = player.GetComponent<CharacterController>();
        if (controller != null)
        {
            StartCoroutine(FreezeControllerOneFrame(controller));
        }
    }

    private IEnumerator FreezeControllerOneFrame(CharacterController controller)
    {
        // Disable movement/velocity for a frame
        Vector3 originalPos = controller.transform.position;
        yield return null; // wait one frame
        controller.transform.position = originalPos;
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