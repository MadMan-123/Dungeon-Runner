using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerBootstrap : NetworkBehaviour 
{
    private PlayerManager players;
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
        players = PlayerManager.instance;
        
        PlayerDataDescriptor data = new()
        {
            index = 0,
            id = NetworkObjectId,
        };

        UpdatePlayerManagerServerRPC(name,data);
    } 
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerManagerServerRPC(string playerName, PlayerDataDescriptor data)
    {
        UpdatePlayerManagerClientRPC(playerName, data);
    }
    
    [ClientRpc]
    private void UpdatePlayerManagerClientRPC(string playerName, PlayerDataDescriptor data)
    {
        if(players.AddPlayer(playerName, data))
        {
            //notify all clients of the new player
            Debug.Log($"[Server]: {playerName} has joined the chat.");

            IEnumerator DelayedVisualUpdate()
            {
                //wait a bit to sync up data
                yield return new WaitForSeconds(0.15f);
                //update the player list
            }
            
            StartCoroutine(DelayedVisualUpdate());
        } 
    }
}
