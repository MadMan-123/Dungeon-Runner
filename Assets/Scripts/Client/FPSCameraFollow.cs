using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;

public class FPSCameraFollow : MonoBehaviour
{
    public float eyeHeight = 1.7f;

    private World clientWorld;
    private EntityManager em; // struct, never null
    private Entity player;
    private EntityQuery playerQuery;
    private float nextRetryTime;
    public float retryInterval = 0.25f; // faster initial acquisition

    // Debug instrumentation (temporary)
    public bool enableDebug = true; // toggle to silence logs
    private float nextDebugTime;
    public float debugInterval = 1f; // seconds between repeated logs
    private int frames;
    private bool loggedPlayerFound;
    private bool listedWorlds;

    void Start()
    {
        // Defer world selection to LateUpdate so we can retry if not ready yet.
        TryAssignClientWorld();
    }

    void LateUpdate()
    {
        frames++;
        if (clientWorld is not { IsCreated: true })
        {
            // Attempt to find client world dynamically.
            TryAssignClientWorld();
            return;
        }


        if (player == Entity.Null)
        {
            if (Time.time >= nextRetryTime)
            {
                nextRetryTime = Time.time + retryInterval;
                FindLocalPlayer();
            }
            if (enableDebug && Time.time >= nextDebugTime)
            {
                nextDebugTime = Time.time + debugInterval;
                int count = playerQuery.CalculateEntityCount();
                Debug.Log("[FPSCameraFollow] Waiting for local ghost. queryCount=" + count);
            }
            return;
        }

        if (!em.Exists(player))
        {
            player = Entity.Null;
            return;
        }

        var t = em.GetComponentData<LocalTransform>(player);
        transform.position = t.Position + new float3(0, eyeHeight, 0);
        transform.rotation = t.Rotation;

        if (enableDebug && !loggedPlayerFound)
        {
            loggedPlayerFound = true;
            Debug.Log("[FPSCameraFollow] Player acquired: EntityIndex=" + player.Index + " after " + frames + " frames");
        }
    }

    void FindLocalPlayer()
    {
        using var entities = playerQuery.ToEntityArray(Unity.Collections.Allocator.Temp);
        if (entities.Length == 0)
            return; // local ghost not spawned yet or not yet enabled

        // First local predicted ghost.
        player = entities[0];
        if (enableDebug)
        {
            Debug.Log("[FPSCameraFollow] FindLocalPlayer(): found entity index=" + player.Index + " totalCandidates=" + entities.Length);
        }
    }

    private void TryAssignClientWorld()
    {
        /*
        if (!listedWorlds && enableDebug)
        {
            listedWorlds = true;
            for (int i = 0; i < World.All.Count; i++)
            {
                var w = World.All[i];
                Debug.Log("[FPSCameraFollow] World index=" + i + " name='" + w.Name + "' flags=" + w.Flags);
            }
        }
        */
        
        
        //World 6 is the client world
        World candidate = World.All[6];
        
        clientWorld = candidate;
        em = clientWorld.EntityManager;
        BuildQuery();

        if (enableDebug)
        {
            Debug.Log("[FPSCameraFollow] Assigned client world='" + clientWorld.Name + "' flags=" + clientWorld.Flags);
        }
    }

    private void BuildQuery()
    {
        if (clientWorld == null || !clientWorld.IsCreated)
            return;
        playerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<GhostOwnerIsLocal>(),
            ComponentType.ReadOnly<LocalTransform>()
        );
        if (enableDebug)
        {
            Debug.Log("[FPSCameraFollow] Query built. Initial count=" + playerQuery.CalculateEntityCount());
        }
    }
}