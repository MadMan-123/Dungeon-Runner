using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Rendering;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[BurstCompile]
public partial struct BulletSystem : ISystem
{
    EntityQuery _bulletQuery;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _bulletQuery = SystemAPI.QueryBuilder()
            .WithAll<LocalTransform, Bullet, ActiveBullet>()
            .WithAll<Simulate>()
            .Build();
    }

    [BurstCompile]
    struct BulletJob : IJobChunk
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        public ComponentTypeHandle<LocalTransform> TransformType;
        public ComponentTypeHandle<Bullet> BulletType;
        public ComponentTypeHandle<MaterialMeshInfo> MeshType;
        public EntityTypeHandle EntityType;
        [BurstCompile]
        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var transforms = chunk.GetNativeArray(ref TransformType);
            var bullets = chunk.GetNativeArray(ref BulletType);
            var meshInfos = chunk.GetNativeArray(ref MeshType);
            var entities = chunk.GetNativeArray(EntityType);

            for (int i = 0; i < chunk.Count; i++)
            {
                var t = transforms[i];
                var b = bullets[i];

                t.Position += b.vel * 10f * DeltaTime;
                b.timer -= DeltaTime;

                if (b.timer <= 0f)
                {
                    // ECB.SetComponentEnabled<Bullet>(unfilteredChunkIndex, entities[i], false);
                    // ECB.SetComponentEnabled<MaterialMeshInfo>(unfilteredChunkIndex, entities[i], false);
                    // ECB.RemoveComponent<ActiveBullet>(unfilteredChunkIndex, entities[i]);
                    ECB.DestroyEntity(unfilteredChunkIndex, entities);
                }

                transforms[i] = t;
                bullets[i] = b;
            }
        }


    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();


        var job = new BulletJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ECB = ecb,
            TransformType = SystemAPI.GetComponentTypeHandle<LocalTransform>(false),
            BulletType = SystemAPI.GetComponentTypeHandle<Bullet>(false),
            MeshType = SystemAPI.GetComponentTypeHandle<MaterialMeshInfo>(false),
            EntityType = SystemAPI.GetEntityTypeHandle()
        };

        state.Dependency = job.ScheduleParallel(_bulletQuery, state.Dependency);

    }

}

