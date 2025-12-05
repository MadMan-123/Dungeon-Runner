using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ClassSelector : MonoBehaviour
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
        var name = LobbyManager.instance.currentName;
        UpdateClassServerRPC(name,currentType);
    }
    [ServerRpc(RequireOwnership = false)]
    private void UpdateClassServerRPC(string nameToUpdate, ClassType type,ServerRpcParams rpcParams = default)
    {
        ulong senderID = rpcParams.Receive.SenderClientId;
        players.playerMap[nameToUpdate].currentClass = type;
        UpdateClassClientRPC(nameToUpdate, type,senderID);
    }

    [ClientRpc]
    private void UpdateClassClientRPC(string nameToUpdate, ClassType type, ulong id)
    {
        players.playerMap[nameToUpdate].currentClass = type;
        

        if (NetworkManager.Singleton.LocalClientId == id)
        {
            LobbyManager.instance.UpdateNameText(); 
        }
        
        
        LobbyManager.instance.UpdatePlayerList();
    }
}
