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
        yield return new WaitForSeconds(0.5f);

        // Stable name derived from OwnerClientId; server is authoritative
        var name = $"Player{OwnerClientId}";
        // Only set the local player's display name on their own client
        if (IsOwner || IsLocalPlayer)
        {
            LobbyManager.instance.currentName = name;
            LobbyManager.instance.UpdateNameText();
        }
        players = PlayerManager.instance;
        
        // Request server-authoritative add
        UpdatePlayerManagerServerRPC(name);
        
    } 
    
    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerManagerServerRPC(string playerName, ServerRpcParams rpcParams = default)
    {
        if (players == null) players = PlayerManager.instance;
        var senderId = rpcParams.Receive.SenderClientId;
        //compute index deterministically 
        var idx = players.playerMap.Count;
        if (players.HasPlayer(playerName))
        {
            // Target only the joining client to align their local state
            var existing = players.GetPlayer(playerName);
            var targetParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
            };
            ApplyAddClientRPC(playerName, existing.index, existing.id,targetParams);
            return;
        }
        var data = new PlayerDataDescriptor
        {
            index = idx,
            id = senderId,
            currentClass = ClassSelector.ClassType.NoOne,
            networkObject = NetworkObject
        };
        //server is the only writer; add and then mirror only to the joining client
        players.AddPlayer(playerName, data);
        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams { TargetClientIds = new[] { senderId } }
        };
        ApplyAddClientRPC(playerName, idx, senderId,clientParams);
    }
    
    [ClientRpc]
    private void ApplyAddClientRPC(string playerName, int index, ulong id, ClientRpcParams clientRpcParams = default)
    {
        if (players == null) players = PlayerManager.instance;
        if (!players.HasPlayer(playerName))
        {
            var data = new PlayerDataDescriptor
            {
                index = index, 
                id = id,
                currentClass = ClassSelector.ClassType.NoOne,
                networkObject = NetworkObject
            };
            players.AddPlayer(playerName, data);
        }

        IEnumerator DelayedVisualUpdate()
        {
            yield return new WaitForSeconds(0.15f);
            LobbyManager.instance.UpdatePlayerList();
            LobbyManager.instance.UpdatePlayerCountText();
            
            LobbyManager.instance.UpdateReadyCountText(LobbyManager.instance.readyCount.Value);
        }
        StartCoroutine(DelayedVisualUpdate());
    }
}
