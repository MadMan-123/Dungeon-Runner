using System.Collections;
using TMPro;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour 
{
    
    public static LobbyManager instance; 
   
    public string currentName = "John";
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI readyCountText;
    public GameObject readyButton;
    public TMP_InputField nameInput;
    public NetworkVariable<int> readyCount = new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public bool localIsReady = false;
    
    private PlayerManager players;

    private bool isHosting = false;
     
    private IEnumerator Start()
    {
        // Wait a frame so PlayerManager.instance is ready after scene load
        yield return null;

        players = PlayerManager.instance;
       
        // Recover correct name from PlayerManager
        string realName = FindMyRealName();
        if (!string.IsNullOrEmpty(realName))
        {
            currentName = realName;
        }
        else
        {
            Debug.LogError("Failed to resync name, using fallback.");
        }

        SyncLobbyUI();
    }

    public void SyncLobbyUI()
    {
        if (players == null)
        {
            players = PlayerManager.instance;
            if (players == null)
            {
                Debug.LogError("LobbyManager could not find PlayerManager instance!");
                return;
            }
        }

        UpdateNameText();
        UpdatePlayerCountText();
        UpdateReadyCountText(readyCount.Value);
        UpdatePlayerList();
    }
 
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void UpdateNameText()
    {
        nameText.text = $"You are: {currentName} a {ClassSelector.instance.currentType.ToString()}";
    }

    public void UpdatePlayerCountText()
    {
        playerCountText.text = $"Connected Players: {players.currentPlayers.Value}";
    }

    public void UpdateReadyCountText(int value)
    {
        readyCountText.text = $"{value}/{players.currentPlayers.Value}";
    }
    private void GetName(string input)
    {
        //update the name on the map
        UpdatePlayerName(currentName, input);
    }
    public void UpdatePlayerName(string oldName, string newName)
    {
        if (players.HasPlayer(newName))
        {
            return;
        }
        UpdatePlayerNameServerRPC(oldName, newName);
        
        currentName = newName; 
        
        UpdateNameText();
        
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdatePlayerNameServerRPC(string oldName, string newName)
    {
        UpdatePlayerNameClientRPC(oldName, newName);
    }

    [ClientRpc]
    private void UpdatePlayerNameClientRPC(string oldName, string newName)
    {
        if (!players.HasPlayer(oldName))
        {
            Debug.LogWarning($"Player {oldName} does not exist");

            return;
        }

        if (players.HasPlayer(newName))
        {
            Debug.LogWarning($"Name {newName} already exists");
            return;
        }

        //remove the old KVP
        var data = players.GetPlayer(oldName);
        players.RemovePlayer(oldName);
       
        //add data with a new name 
        players.AddPlayer(newName, data);
        players.currentPlayers.Value--; 
        UpdatePlayerList();
        UpdatePlayerCountText();
    }

    public void UpdatePlayerList()
    {
        //get all the keys and add to one big string seperated with \n
        var output = "";

        playerListText.text = "";
        foreach (var kv in players.playerMap)
        {
            var data = players.GetPlayer(kv.Key);
            output += $"{kv.Key}:{data.currentClass.ToString()}\n";
        }

        playerListText.text = output;
    }
    public void ReadyUp()
    {
        localIsReady = !localIsReady;
 
        //sync the ready count ui with all clients
        RequestReadyUpdateServerRPC(localIsReady);
    }
    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyUpdateServerRPC(bool isReady)
    {
        readyCount.Value += isReady ? 1 : -1;
        UpdateReadyClientRPC(readyCount.Value);
        
        if (readyCount.Value == players.currentPlayers.Value)
        {
            //load world
            Debug.Log("Load World Scene for all clients");
            readyButton.SetActive(true); 
            return;
        }
        
        readyButton.SetActive(false); 
    }

  
    [ClientRpc]
    public void UpdateReadyClientRPC(int value)
    {
        
        //update the UI after a few secconds
        IEnumerator delay()
        {
            yield return new WaitForSeconds(0.25f);
            UpdateReadyCountText(value);
        }

        StartCoroutine(delay());
    }

    private string FindMyRealName()
    {
        ulong local = NetworkManager.Singleton.LocalClientId;

        foreach (var kv in players.playerMap)
        {
            if (kv.Value.id == local)
                return kv.Key;
        }

        Debug.LogError("LobbyManager: Could not find local player's name in PlayerManager!");
        return null;
    }

    public void StartGame()
    {
        var manager = (FPSManager)(NetworkManager.Singleton);
        manager.LoadWorld();
    }
}
