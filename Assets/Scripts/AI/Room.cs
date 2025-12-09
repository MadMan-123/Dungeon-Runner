using Unity.Netcode;
using UnityEngine;

public class Room : NetworkBehaviour 
{
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
    public Type type;
}
