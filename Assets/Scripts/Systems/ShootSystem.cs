using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[BurstCompile]
partial struct ShootSystem : ISystem, ISystemStartStop
{
    private EntitiesReferences entitiesReferences;
    private NetworkTime nTime;
    private NativeArray<Entity> bulletArray;
    private int bulletPoolSize;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        bulletPoolSize = 1000;
        //nextFreeBulletIndex = 0;
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkTime>();

    }

    [BurstCompile]
    void ISystemStartStop.OnStartRunning(ref SystemState state)
    {
        entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        nTime = SystemAPI.GetSingleton<NetworkTime>();

        bulletArray = new NativeArray<Entity>(bulletPoolSize, Allocator.Persistent);

        for (int i = 0; i < bulletPoolSize; i++)
        {
            Entity e = state.EntityManager.Instantiate(entitiesReferences.BulletPrefabEntity);

            state.EntityManager.SetComponentData(e, new LocalTransform
            {
                Position = new float3(0f, -9999f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            state.EntityManager.SetComponentData(e, new Bullet
            {
                timer = 0f,
                vel = float3.zero
            });

            state.EntityManager.SetComponentData(e, new GhostOwner { NetworkId = 0 });

            // Disable bullet component to remove from simulation and hide renderable
            state.EntityManager.SetComponentEnabled<Bullet>(e, false);
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(e, false);

            bulletArray[i] = e;

        }
    }

    [BurstCompile]
    void ISystemStartStop.OnStopRunning(ref SystemState state)
    {
        if (bulletArray.IsCreated)
            bulletArray.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        int bulletsPerShot = 100;

        var claimed = new NativeArray<byte>(bulletPoolSize, Allocator.Temp);

        foreach (var (npi, localTransform, owner) in
             SystemAPI.Query<
                 RefRO<NetcodePlayerInput>,
                 RefRO<LocalTransform>,
                 RefRO<GhostOwner>>()
             .WithAll<Simulate>())
        {
            //check if shooting can be performed
            if (!npi.ValueRO.shoot.IsSet)
                continue;


            //shoot multiple bullets
            for (int i = 0; i < bulletsPerShot; i++)
            {
                //get a bullet entity
                Entity bullet = GetPooledBullet(ref state,ref ecb, ref claimed);

                //sanity check
                if (bullet == Entity.Null)
                    break; // out of pool

                //work out the direction needed to launch in a circle
                float angle = math.radians((360f / bulletsPerShot) * i);
                float3 offset = new float3(math.cos(angle), 0, math.sin(angle)) * 0.5f;
                float3 dir = math.normalize(offset);

                //update transform data
                ecb.SetComponent(bullet, new LocalTransform
                {
                    Position = localTransform.ValueRO.Position + offset,
                    Rotation = quaternion.LookRotationSafe(dir, math.up()),
                    Scale = 1f
                });

                //update bullet data
                ecb.SetComponent(bullet, new Bullet
                {
                    timer = 5f,
                    vel = dir
                });

                //update the network id to be the current owner
                ecb.SetComponent(bullet, new GhostOwner { NetworkId = owner.ValueRO.NetworkId });

                // When shooting
                ecb.SetComponentEnabled<Bullet>(bullet, true);
                ecb.SetComponentEnabled<MaterialMeshInfo>(bullet, true);
                ecb.AddComponent<ActiveBullet>(bullet);


            }
        }

        //clean up
        ecb.Playback(state.EntityManager);
        ecb.Dispose();

        claimed.Dispose();
    }

    [BurstCompile]
    private Entity GetPooledBullet(ref SystemState state, ref EntityCommandBuffer ecb, ref NativeArray<byte> claimed)
    {
        var mgr = state.EntityManager;
        for (int i = 0; i < bulletPoolSize; i++)
        {
            Entity e = bulletArray[i];

            if (!mgr.Exists(e))
                continue;
            if (!mgr.HasComponent<Bullet>(e))
                continue;
            if (!mgr.IsComponentEnabled<Bullet>(e) && claimed[i] == 0)
            {
                claimed[i] = 1;
                return e;
            }

        }

        return Entity.Null;
    }
/*
    private int nextFreeBulletIndex;

    private Entity GetPooledBullet(ref SystemState state)
    {
        var mgr = state.EntityManager;
        for (int i = 0; i < bulletPoolSize; i++)
        {
            int idx = (nextFreeBulletIndex + i) % bulletPoolSize;
            Entity e = bulletArray[idx];
            if (!mgr.IsComponentEnabled<Bullet>(e))
            {
                nextFreeBulletIndex = (idx + 1) % bulletPoolSize;
                return e;
            }
        }
        return Entity.Null;
    }*/


    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (bulletArray.IsCreated)
            bulletArray.Dispose();
    }
}
