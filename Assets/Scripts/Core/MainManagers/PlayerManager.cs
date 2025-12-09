using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataDescriptor : INetworkSerializable 
{
    public int index;
    public ulong id;
    public ClassSelector.ClassType currentClass;
    public NetworkObject networkObject;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref index);
        serializer.SerializeValue(ref id);
        serializer.SerializeValue(ref currentClass);
        
        
    }
}

public class PlayerManager : NetworkBehaviour
{
    public static PlayerManager instance;
    public Dictionary<string, PlayerDataDescriptor> playerMap = new();
    public int maxPlayers = 4;
    public NetworkVariable<int> currentPlayers = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public bool isPersistentRuntimeInstance = false;

    private void Awake()
    {
        if (instance == null)
        {
            // First ever creation 
            instance = this;
            DontDestroyOnLoad(gameObject);
            isPersistentRuntimeInstance = true;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        } 
        
       
    }

    public void ValidateSingleton()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // This is the true instance
        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (isPersistentRuntimeInstance)
            instance = this; 
    }

  
    public bool AddPlayer(string playerName, PlayerDataDescriptor data)
    {
        
        //try check if the map contains the name already
        if (playerMap.TryAdd(playerName, data))
        {
            Debug.Log($"Added player: {playerName} to map");
            currentPlayers.Value++;

            return true;
        }
        //debug that we cant add
        Debug.LogWarning($"Player: {playerName} is already in the session");
        return false;


    }

    /*public void UpdatePlayerClass(string playerName, ClassSelector.ClassType newClass)
    {
        if (!playerMap.TryGetValue(playerName, out var data))
        {
            Debug.LogWarning($"Cannot update class — player '{playerName}' not found.");
            return;
        }

        data.currentClass = newClass;
    }*/

    
    public bool RemovePlayer(string playerName)
    {
        if (!HasPlayer(playerName))
        {
            Debug.LogWarning($"there is no {playerName} in the map");
            return false;
        }
        Debug.Log($"Removing player: {playerName} from map");
        playerMap.Remove(playerName);

        return true;
    }

    public bool HasPlayer(string playerName)
    {
        return playerMap.ContainsKey(playerName);
    }

    public PlayerDataDescriptor GetPlayer(string playerName)
    {
        if (!HasPlayer(playerName))
        {
            //cannot get 
            Debug.LogWarning($"{playerName} does not exist");

        }
        return playerMap[playerName];
    }


    public PlayerDataDescriptor GetPlayerById(ulong senderID)
    {
        foreach (var p in playerMap.Values)
        {
            if (p.id == senderID)
                return p;
        }

        Debug.LogWarning($"No player with id {senderID} found");
        return null;
    }

    
    public string GetNameByData(PlayerDataDescriptor data)
    {
        return playerMap
            .FirstOrDefault(kvp => kvp.Value.id == data.id)
            .Key;
    }


    public string GetNameById(ulong sender)
    {
        return playerMap.FirstOrDefault(kvp => kvp.Value.id == sender).Key;
    }


}
