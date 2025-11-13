using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Rendering;
using Unity.Transforms;
using Unity.Jobs;
[BurstCompile]
partial struct AgentManager : ISystem
{
    /*EntityQuery _query;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _query = SystemAPI.QueryBuilder()
            .WithAll<Agent, LocalTransform>()
            .Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!state.WorldUnmanaged.IsServer()) return;

        float dt = SystemAPI.Time.DeltaTime;
        var ecb = SystemAPI
            .GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>()
            .CreateCommandBuffer(state.WorldUnmanaged)
            .AsParallelWriter();

        var job = new AgentJob { DeltaTime = dt, ECB = ecb };
        state.Dependency = job.ScheduleParallel(_query, state.Dependency);
    }

    [BurstCompile]
    partial struct AgentJob : IJobChunk
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;
    

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            throw new System.NotImplementedException();
        }
    }*/
}
