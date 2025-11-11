using Unity.Entities;
using Unity.NetCode;

public static class RpcUtils
{
    public static void Send<T>(ref EntityManager em, T rpcData, Entity target = default)
        where T : unmanaged, IRpcCommand
    {
        var e = em.CreateEntity();
        em.AddComponentData(e, rpcData);
        em.AddComponentData(e, new SendRpcCommandRequest { TargetConnection = target });
    }
    
    public static void Send<T>(EntityCommandBuffer ecb,T data ,Entity target = default)
        where T : unmanaged, IRpcCommand
    {
        var e = ecb.CreateEntity();
        ecb.AddComponent(e, data);
        ecb.AddComponent(e, new SendRpcCommandRequest { TargetConnection = target });
    }
}