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


    public override void OnNetworkSpawn()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }
        base.OnNetworkSpawn();
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
        var player = playerMap.Values.ToList().Find(p => p.id == senderID);
        if (player != null) return player;

        Debug.LogWarning($"No player with id {senderID} found");
        return null;
    }

   
}