using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct NetcodePlayerMovmentSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (netcodePlayerInput, localTransform) in SystemAPI.Query<RefRO<NetcodePlayerInput>,RefRW<LocalTransform>>().WithAll<Simulate>())
        {
            float3 moveVec = new(netcodePlayerInput.ValueRO.InputVec.x, 0, netcodePlayerInput.ValueRO.InputVec.y);
            float moveSpeed = 10;
            localTransform.ValueRW.Position += moveVec * moveSpeed * SystemAPI.Time.DeltaTime;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
