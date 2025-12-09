using Core;
using Unity.Netcode;
using UnityEngine;

public class GoBackToLobby : NetworkBehaviour 
{
    public void GoBack(NetworkObject networkObject)
    {
        if (IsServer)
        {
            Loader.LoadNetwork(Loader.Scene.Lobby);
        }
        
        
    }
}
