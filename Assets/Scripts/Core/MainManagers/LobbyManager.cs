using System.Collections;
using TMPro;
using System.Linq;
using Core;
using Unity.Netcode;
using UnityEngine;

public class LobbyManager : NetworkBehaviour
{
    // Singleton instance
    public static LobbyManager instance;

    // Local player's name
    public string currentName = "John";

    // UI elements
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI playerListText;
    public TextMeshProUGUI playerCountText;
    public TextMeshProUGUI readyCountText;
    public GameObject readyButton;
    public TMP_InputField nameInput;

    // How many players are ready
    public NetworkVariable<int> readyCount =
        new(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    // Small local flag for ready state
    public bool localIsReady = false;

    private PlayerManager players;
    private bool isHosting = false;

    private IEnumerator Start()
    {
        // Wait one frame so PlayerManager is loaded
        yield return null;

        players = PlayerManager.instance;

        // Grab the correct synced name
        string realName = FindMyRealName();
        if (!string.IsNullOrEmpty(realName))
            currentName = realName;
        else
            Debug.LogError("Failed to resync name, using fallback.");

        SyncLobbyUI();

        // When the user presses enter in the name box
        nameInput.onSubmit.AddListener(GetName);
    }

    public void SyncLobbyUI()
    {
        // Make sure PlayerManager exists
        if (players == null)
        {
            players = PlayerManager.instance;
            if (players == null)
            {
                Debug.LogError("LobbyManager: PlayerManager missing!");
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
        // Simple singleton logic
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }

    public void UpdateNameText()
    {
        nameText.text = $"You are: {currentName} a {ClassSelector.instance.currentType}";
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
        // Replace old name with new one
        UpdatePlayerName(currentName, input);
    }

    public void UpdatePlayerName(string oldName, string newName)
    {
        // Don’t allow duplicates
        if (players.HasPlayer(newName))
            return;

        // Tell server to update name
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
            Debug.LogWarning($"Player {oldName} doesn't exist");
            return;
        }

        if (players.HasPlayer(newName))
        {
            Debug.LogWarning($"Name {newName} is taken");
            return;
        }

        // Grab old data
        var data = players.GetPlayer(oldName);

        // Remove old and re-add with new name
        players.RemovePlayer(oldName);
        players.AddPlayer(newName, data);

        players.currentPlayers.Value--;

        UpdatePlayerList();
        UpdatePlayerCountText();
    }

    public void UpdatePlayerList()
    {
        // Build simple list of names + class
        string output = "";
        playerListText.text = "";

        foreach (var kv in players.playerMap)
        {
            var data = players.GetPlayer(kv.Key);
            output += $"{kv.Key}: {data.currentClass}\n";
        }

        playerListText.text = output;
    }

    public void ReadyUp()
    {
        // Flip ready state
        localIsReady = !localIsReady;

        // Update everyone
        RequestReadyUpdateServerRPC(localIsReady);
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestReadyUpdateServerRPC(bool isReady)
    {
        // Adjust ready count
        readyCount.Value += isReady ? 1 : -1;

        // Update UI on clients
        UpdateReadyClientRPC(readyCount.Value);

        // If everyone is ready
        if (readyCount.Value == players.currentPlayers.Value)
        {
            Debug.Log("Load World Scene for all clients");
            readyButton.SetActive(true);
            return;
        }

        readyButton.SetActive(false);
    }

    [ClientRpc]
    public void UpdateReadyClientRPC(int value)
    {
        // Small delay so UI doesn't fight with network updates
        IEnumerator delay()
        {
            yield return new WaitForSeconds(0.25f);
            UpdateReadyCountText(value);
        }

        StartCoroutine(delay());
    }

    private string FindMyRealName()
    {
        // Find the map entry that matches this client's ID
        ulong local = NetworkManager.Singleton.LocalClientId;

        foreach (var kv in players.playerMap)
        {
            if (kv.Value.id == local)
                return kv.Key;
        }

        Debug.LogError("Couldn't find local player's name!");
        return null;
    }

    public void StartGame()
    {
        // FPSManager handles the scene load
        var manager = (FPSManager)(NetworkManager.Singleton);
        manager.LoadWorld();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerServerRPC(string playerName, ServerRpcParams rpcParams = default)
    {
        if (players == null) players = PlayerManager.instance;

        if (!players.HasPlayer(playerName))
            return;

        players.RemovePlayer(playerName);
        players.currentPlayers.Value--;

        var clientParams = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = NetworkManager.Singleton.ConnectedClientsIds.ToArray()
            }
        };

        ApplyRemoveClientRPC(playerName, clientParams);
    }

    [ClientRpc]
    private void ApplyRemoveClientRPC(string playerName, ClientRpcParams clientRpcParams = default)
    {
        if (players == null) players = PlayerManager.instance;

        if (players.HasPlayer(playerName))
            players.RemovePlayer(playerName);

        // Wait a moment so UI doesn't desync
        IEnumerator DelayedVisualUpdate()
        {
            yield return new WaitForSeconds(0.15f);
            UpdatePlayerList();
            UpdatePlayerCountText();
            UpdateReadyCountText(readyCount.Value);
        }

        StartCoroutine(DelayedVisualUpdate());
    }

    public void LeaveGame()
    {
        // Remove from lobby and leave network session
        RemovePlayerServerRPC(currentName);
        NetworkManager.Singleton.Shutdown();
        Destroy(players.gameObject);

        Loader.Load(Loader.Scene.MainMenu);
    }
}
