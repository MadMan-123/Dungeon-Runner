using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct TestEntitiesServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Unity.Collections.Allocator.Temp);
        foreach (var (
                     simpleRPC, 
                     receiveRpcCommandRequest,
                     entity) 
                        in SystemAPI.Query<
                            RefRO<SimpleRPC>, 
                            RefRO<ReceiveRpcCommandRequest>>().WithEntityAccess()) 
        {
            
            //Debug.Log("Received RPC" + simpleRPC.ValueRO.Value + " :: " + receiveRpcCommandRequest.ValueRO.SourceConnection);
            ecb.DestroyEntity(entity);
        }
        ecb.Playback(state.EntityManager);
        

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
