using Unity.NetCode;
using UnityEngine;


[UnityEngine.Scripting.Preserve]
public class Bootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        //set the port
        AutoConnectPort = 7989;
        var ok = base.Initialize(defaultWorldName); 
        Application.targetFrameRate = 170;


        return ok;
    }
}
