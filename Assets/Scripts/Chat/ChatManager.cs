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
    [SerializeField] TextMeshProUGUI playerCount;
    bool hasName = false;
    const int MAX_CHATS = 1024;
    private ChatMessage[] chatMessages;
    TextManager manager;
    [SerializeField] PlayerManager players;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
          
        manager = (TextManager)NetworkManager.Singleton;
        playerCount.text = $"Connected: {players.currentPlayers.Value}";
        
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
    private void OnHidePanel()
    {
        isShowing = !isShowing;
        chatContainer.SetActive(isShowing);
    }

    //on enter being pressed
    void OnEnterPressed(string input)
    {
        
        if (!hasName)
            GetPlayersName();
        else
            Chat(input);
    }

    private void Chat(string input)
    {

        SendChatMessageServerRPC(input, manager.LocalClientId);
        chatInput.text = "";
    }

    private void AskForPlayersName()
    {
        //add a chat message from the server
        string message = "[Server]: Please Enter your name";

        AddMessage(message);

    }
    private void GetPlayersName()
    {


        var inputName = chatInput.text;

        if (players.HasPlayer(inputName))
        {
            AddMessage($"Name {inputName} has already been taken, retry");
            AskForPlayersName();
            return;
        }

       

        hasName = true;


        //notify the player
        AddMessage($"Welcome {inputName} to the chat!");

        //clear the input field
        chatInput.text = "";

        //notify the server to update other clients
        NotifyServerOfNewPlayerServerRPC(inputName, manager.LocalClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void NotifyServerOfNewPlayerServerRPC(string playerName, ulong playerId)
    {
        //broadcast the new player data to all clients
        NotifyClientsOfNewPlayerClientRPC(playerName, playerId, players.currentPlayers.Value++);
    }

    [ClientRpc]
    private void NotifyClientsOfNewPlayerClientRPC(string playerName, ulong playerId, int playerIndex)
    {
        //update the players on all clients
        PlayerDataDescriptor data = new PlayerDataDescriptor
        {
            id = playerId,
            name = playerName,
            index = playerIndex
        };

        if(players.AddPlayer(playerName, data))
        {
            //notify all clients of the new player
            AddMessage($"[Server]: {playerName} has joined the chat.");

            //wait a delay for the value to sync
            IEnumerator DelayedUpdate()
            {
                yield return new WaitForSeconds(0.1f);
                //set the player count 
                playerCount.text = $"Connected: {players.currentPlayers.Value}";
            }
            
            StartCoroutine(DelayedUpdate());
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void SendChatMessageServerRPC(string message,ulong senderID)
    {
        if (string.IsNullOrEmpty(message))
        {
            Debug.LogWarning("Empty message");
            return;
        }
        
        //get the player name from the player manager
        var data = players.GetPlayerById(senderID);
        //validate
        if (data == null)
        {
            Debug.LogError("Player data is null");
            return;
        }
        //send to all clients
        SendChatMessageClientRPC(message,data.name,senderID);
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
