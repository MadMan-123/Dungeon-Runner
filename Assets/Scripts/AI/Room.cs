using Unity.Netcode;
using UnityEngine;

public class Room : NetworkBehaviour 
{
    //Basic Descriptor for rooms and handling how they anchor one to another
    public enum Type
    {
        MainHub,
        Corridor,
        Room,
        BossRoom,
        MaxRoom
    }
    
    public Transform AnchorStart;
    public Transform AnchorEnd;
    public NetworkObject ExitDoor;
    public Type type;
}
