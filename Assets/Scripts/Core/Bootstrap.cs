using Unity.NetCode;
using UnityEngine;


[UnityEngine.Scripting.Preserve]
public class Bootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        //set the port
        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
}
