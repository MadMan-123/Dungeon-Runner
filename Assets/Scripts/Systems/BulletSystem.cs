using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]

partial struct BulletSystem : ISystem
{
    EntityCommandBuffer ecb;
    private ShootSystem system;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        
        ecb = new(Unity.Collections.Allocator.Temp);
        foreach (var (localTransform, bullet, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<Bullet>>()
                         .WithAll<Simulate>()
                         .WithAll<ActiveBullet>()
                         .WithEntityAccess())
        {
            if (!state.WorldUnmanaged.IsServer())
                continue;

            float3 vel = bullet.ValueRO.vel;
            float moveSpeed = 10f;
            localTransform.ValueRW.Position += vel * moveSpeed * SystemAPI.Time.DeltaTime; 
        
            
            
            bullet.ValueRW.timer -= SystemAPI.Time.DeltaTime;
            if (bullet.ValueRW.timer <= 0f)
            {
                ecb.SetComponentEnabled<Bullet>(entity, false);
                ecb.SetComponentEnabled<MaterialMeshInfo>(entity, false);
                ecb.RemoveComponent<ActiveBullet>(entity);
            }



        }
        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
