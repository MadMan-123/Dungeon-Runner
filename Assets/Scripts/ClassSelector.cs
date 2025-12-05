using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ClassSelector : NetworkBehaviour 
{
    
    public ClassType currentType = ClassType.NoOne;
    public static ClassSelector instance;
    private PlayerManager players;
    public enum ClassType
    {
        NoOne = -1,
        Wizard,
        Knight,
        Ranger,
        MaxClass
    }


    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }
    }

    private void Start()
    {
        players = PlayerManager.instance;
    }


    public void SetType(int index)
    {
        if(index is > (int)ClassType.MaxClass or < 0)
            return;
        
        currentType = (ClassType)index;
    }

    public void UpdateClass()
    {
        UpdateClassServerRPC(LobbyManager.instance.currentName,currentType);
        LobbyManager.instance.UpdateNameText(); 
        
    }
    [ServerRpc(RequireOwnership = false)]
    private void UpdateClassServerRPC(string nameToUpdate,ClassType type,ServerRpcParams rpcParams = default)
    {
        if (!players.HasPlayer(nameToUpdate))
        {
            Debug.LogWarning($"SERVER - Cannot update {nameToUpdate}, player not found");
            return;
        } 
        // Update server data
        var data = players.playerMap[nameToUpdate];
        data.currentClass = type;
        players.playerMap[nameToUpdate] = data;

        Debug.Log($"SERVER - Updating {nameToUpdate} with class: {type.ToString()}");

        // Update host UI immediately
        if (IsHost)
        {
            LobbyManager.instance.UpdatePlayerList();
            if (nameToUpdate == LobbyManager.instance.currentName)
                LobbyManager.instance.UpdateNameText();
        } 
        UpdateClassClientRPC(nameToUpdate, type); 
      
    }

    [ClientRpc]
    private void UpdateClassClientRPC(string nameToUpdate, ClassType type)
    {
        if (!players.HasPlayer(nameToUpdate))
        {
            Debug.LogWarning($"Missing player '{nameToUpdate}'");
            return; 
        }
        players.playerMap[nameToUpdate].currentClass = type;
        Debug.Log($"CLIENT - Updating {nameToUpdate} with class: {type.ToString()}");
        
        LobbyManager.instance.UpdatePlayerList();
    }
}
