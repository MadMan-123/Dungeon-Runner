using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerBootstrap : NetworkBehaviour 
{
    public override void OnNetworkSpawn()
    {
        var routine = WaitAndSpawn();
        StartCoroutine(routine);
        
        
        base.OnNetworkSpawn();
    }
    IEnumerator WaitAndSpawn()
    {
        //this is bullshit
        yield return new WaitForSeconds(0.75f);

        var name = $"Player{PlayerManager.instance.currentPlayers.Value}";
        LobbyManager.instance.currentName = name;
        LobbyManager.instance.UpdateNameText();

        PlayerDataDescriptor data = new()
        {
            index = 0,
            id = NetworkObjectId,
        };

        PlayerManager.instance.AddPlayer(name, data);

    } 
}
