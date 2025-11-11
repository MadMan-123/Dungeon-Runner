using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;

partial struct AgentManager : ISystem, ISystemStartStop
{

    EntityCommandBuffer ecb;

    private EntitiesReferences entitiesReferences;
    private NetworkTime nTime;
    private NativeArray<Entity> agents;
    private int agentPoolSize;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkTime>();

        agentPoolSize = 100;
        //create all agents upfront

    }


    float timer;
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        timer++;

        ecb = new(Allocator.Temp);
        
        foreach (var (localTransform, agent, entity) in
                SystemAPI.Query<RefRW<LocalTransform>, RefRW<Agent>>()
                         .WithAll<Simulate>()
                         .WithAll<ActiveBullet>()
                         .WithEntityAccess())
        {
            if (!state.WorldUnmanaged.IsServer())
                continue;




        }
        ecb.Playback(state.EntityManager);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }

    public void OnStartRunning(ref SystemState state)
    {
        entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        nTime = SystemAPI.GetSingleton<NetworkTime>();

        agents = new NativeArray<Entity>(agentPoolSize, Allocator.Persistent);

        for (int i = 0; i < agentPoolSize; i++)
        {
            Entity e = state.EntityManager.Instantiate(entitiesReferences.BulletPrefabEntity);

            state.EntityManager.SetComponentData(e, new LocalTransform
            {
                Position = new float3(0f, -9999f, 0f),
                Rotation = quaternion.identity,
                Scale = 1f
            });

            //state.EntityManager.SetComponentData(e, new Agent
            //{

            //});

            state.EntityManager.SetComponentData(e, new GhostOwner { NetworkId = 0 });

            // Disable bullet component to remove from simulation and hide renderable
            state.EntityManager.SetComponentEnabled<Bullet>(e, false);
            state.EntityManager.SetComponentEnabled<MaterialMeshInfo>(e, false);

            agents[i] = e;

        }
    }

    public void OnStopRunning(ref SystemState state)
    {
        throw new System.NotImplementedException();
    }
}
