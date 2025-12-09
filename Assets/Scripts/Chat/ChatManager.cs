using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ChatManager : NetworkBehaviour
{
    #region Chat
    //Chat info
    [SerializeField] ChatMessage prefab;
    [SerializeField] CanvasGroup chatContent;
    [SerializeField] TMP_InputField chatInput;
    [SerializeField] Button hideButton;
    [SerializeField] GameObject chatContainer;
    bool hasName = false;
    const int MAX_CHATS = 1024;
    private ChatMessage[] chatMessages;
    FPSManager manager;
    [SerializeField] PlayerManager players;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
          
        manager = (FPSManager)NetworkManager.Singleton;
        
        if (players == null)
        {
            Debug.LogError("ChatManager could not find PlayerManager!");
            return;
        } 
        localName = GetLocalPlayerName();
 
        
        chatMessages = new ChatMessage[MAX_CHATS];
        
        Transform parent = chatContent.transform;

        //instantiate each object
        for (int i = 0; i < MAX_CHATS; i++)
        {
            GameObject obj = Instantiate(prefab.gameObject, parent, true);
            if (!obj.TryGetComponent(out chatMessages[i]))
            {
                Debug.LogError("Has no component of ChatMessage");
                continue;
            }
            //set the rect transform
            RectTransform rect = obj.GetComponent<RectTransform>();
            
            rect.localScale = Vector3.one;
            var cache = rect.localPosition;
            cache.z = 0;
            rect.localPosition = cache;
            
            obj.SetActive(false);
        }
        AskForPlayersName();
        chatInput.onSubmit.AddListener(OnEnterPressed);
        hideButton.onClick.AddListener(OnHidePanel);
    }

    private bool isShowing = true;
    private string localName;

    private void OnHidePanel()
    {
        isShowing = !isShowing;
        chatContainer.SetActive(isShowing);
    }

    //on enter being pressed
    private void OnEnterPressed(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return;

        Chat(input); 
       
    }
    private void Chat(string input)
    {
        SendChatMessageServerRPC(input, LobbyManager.instance.currentName,manager.LocalClientId);
        chatInput.text = "";
    }

    private void AskForPlayersName()
    {
        //add a chat message from the server
        string message = "[Server]: Please Enter your name";

        AddMessage(message);

    }

    private string GetLocalPlayerName()
    {
        ulong localId = NetworkManager.Singleton.LocalClientId;

        foreach (var kv in players.playerMap)
        {
            if (kv.Value.id == localId)
                return kv.Key;
        }

        return null;
    }



    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRPC(string message,string name,ulong senderID)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("Empty message");
            return;
        }

        
        //send to all clients
        SendChatMessageClientRPC(message,name,senderID);
    }

    [ClientRpc]
    public void SendChatMessageClientRPC(string message,string playerName, ulong senderID)
    {
        //add the message to the chat
        AddMessage($"[{playerName}]:{message}");
    }

    public void AddMessage(string message)
    {
        //get one of the messages
        var m = GetMessage();

        m.SetMessage(message);
    }

    private ChatMessage GetMessage()
    {
        for (int i = 0; i < MAX_CHATS; i++)
        {
            var message = chatMessages[i];
            if (!message.isActiveAndEnabled)
            {
                message.gameObject.SetActive(true);
                return message;
            }
        }

        Debug.LogError("No more chats to use");
        return null;
    }
    #endregion
}
