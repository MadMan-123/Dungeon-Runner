using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.NetCode;

[BurstCompile]
public partial struct ShootSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var entitiesRefs = SystemAPI.GetSingleton<EntitiesReferences>();

        int bulletsPerShot = 25;
        float radius = 0.5f;

        foreach (var (npi, localTransform, owner) in
                 SystemAPI.Query<
                     RefRO<NetcodePlayerInput>,
                     RefRO<LocalTransform>,
                     RefRO<GhostOwner>>()
                 .WithAll<Simulate>())
        {
            if (!npi.ValueRO.shoot.IsSet)
                continue;

            for (int i = 0; i < bulletsPerShot; i++)
            {
                Entity bullet = ecb.Instantiate(entitiesRefs.BulletPrefabEntity);

                float angle = math.radians((360f / bulletsPerShot) * i);
                float3 offset = new float3(math.cos(angle), 0, math.sin(angle)) * radius;
                float3 dir = math.normalize(offset);

                ecb.SetComponent(bullet, new LocalTransform
                {
                    Position = localTransform.ValueRO.Position + offset,
                    Rotation = quaternion.LookRotationSafe(dir, math.up()),
                    Scale = 1f
                });

                ecb.SetComponent(bullet, new Bullet
                {
                    vel = dir,
                    timer = 5f
                });

                ecb.SetComponent(bullet, new GhostOwner { NetworkId = owner.ValueRO.NetworkId });
                ecb.AddComponent<ActiveBullet>(bullet);
            }
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

