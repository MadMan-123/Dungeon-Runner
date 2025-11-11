using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;


[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EntitiesReferences>();
        state.RequireForUpdate<NetworkId>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new(Unity.Collections.Allocator.Temp);
        
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        foreach (var (rpc, entity) in
                 SystemAPI.Query<
                     RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRPC>().WithEntityAccess())
        {
            ecb.AddComponent<NetworkStreamInGame>(rpc.ValueRO.SourceConnection);
            ecb.DestroyEntity(entity);
            
            //spawn player prefab
            Entity player = ecb.Instantiate(entitiesReferences.PlayerPrefabEntity);
            //set to random pos on x axis
            ecb.SetComponent(player,LocalTransform.FromPosition((new float3(UnityEngine.Random.Range(-10,+10),0,0))));

            //get network id
            NetworkId id = SystemAPI.GetComponent<NetworkId>(rpc.ValueRO.SourceConnection);
            
            //add the id value
            ecb.AddComponent(player,new GhostOwner
            {
                NetworkId = id.Value,
            });
        }
        ecb.Playback(state.EntityManager);
    }
    

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        
    }
}
