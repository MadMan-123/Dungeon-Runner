using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial struct GoInGameClientSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Unity.Collections.Allocator.Temp);

        //This is the pattern to get data,
        foreach (var (networkId, entity) in 
                 (SystemAPI.Query<RefRO<NetworkId>>()
                     .WithNone<NetworkStreamInGame>()
                     .WithEntityAccess()))
        {
            ecb.AddComponent<NetworkStreamInGame>(entity);
        
            RpcUtils.Send(ecb, new GoInGameRequestRPC(),entity);
        }
        ecb.Playback(state.EntityManager);
       
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}



public struct GoInGameRequestRPC : IRpcCommand
{
    
}