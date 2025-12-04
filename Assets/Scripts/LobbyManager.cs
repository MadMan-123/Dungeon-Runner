using TMPro;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour 
{
    
    public static LobbyManager instance; 
   
    public string currentName = "John";
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playerListText;
    public TMP_InputField nameInput;
    public NetworkVariable<int> readyCount;

    private PlayerManager players;

    private bool isHosting = false;
    
    private void Start()
    {
        nameInput.onSubmit.AddListener(GetName); 
        UpdateNameText();
        players = PlayerManager.instance;
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
    public void UpdateNameText()
    {
        nameText.text = $"You are: {currentName} a {ClassSelector.instance.currentType.ToString()}";
    }
    

    
    private void GetName(string input)
    {
        //update the name on the map
        UpdatePlayerName(currentName, input);
        UpdateNameText();
    }
    public void UpdatePlayerName(string oldName, string newName)
    {
        UpdatePlayerNameServerRPC(oldName, newName);
        currentName = newName;  
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
        //compensate for changing the data
        players.currentPlayers.Value--;
       
        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        //get all the keys and add to one big string seperated with \n
        var output = "";

        playerListText.text = "";
        foreach (var key in players.playerMap.Keys)
        {
            output += key + "\n";
        }

        playerListText.text = output;
    }
    public void ReadyUp()
    {
        var manager = (FPSManager)(NetworkManager.Singleton);
        
        manager.LoadWorld();
    }
}
