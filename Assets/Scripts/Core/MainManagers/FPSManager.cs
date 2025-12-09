using System;
using System.Collections;
using System.Collections.Generic;
using Core;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class FPSManager : NetworkManager
{
    private void Start()
    {
        DontDestroyOnLoad(gameObject); 
        
        
        OnClientConnectedCallback += OnClientConnected;
        OnServerStarted += OnServerStart;
    }

    private void OnServerStart()
    {
        NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadedForAll;
 
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientId == LocalClientId)
        {
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += OnSceneLoadedForAll;
        } 
    }

    public void Startlobby()
    {
        //host and load the Main scene
        StartHost();
        Debug.Log($"[FPSManager] Host started. IsServer={IsServer}, IsHost={IsHost}, IsClient={IsClient}");
        Loader.LoadNetwork(Loader.Scene.Lobby);
        
    }

    public void JoinLobby()
    {
        //TODO: List all hosted games
        if (StartClient())
        {
            Debug.Log($"[FPSManager] Client started. IsServer={IsServer}, IsHost={IsHost}, IsClient={IsClient}");
        } 

    }
  
    public void LoadWorld()
    {
        if (!IsServer)
        {
            Debug.LogWarning("Only server can load scenes");
            return;
        }
        Loader.LoadNetwork(Loader.Scene.World);
    }
    private void OnSceneLoadedForAll(string sceneName, LoadSceneMode mode,
        List<ulong> clientsComplete, List<ulong> clientsTimedOut)
    {
        Debug.Log($"[FPSManager] Scene load completed for '{sceneName}'. " +
                  $"IsServer={IsServer}, IsHost={IsHost}, IsClient={IsClient}. " +
                  $"Clients complete: {clientsComplete.Count}, timed out: {clientsTimedOut.Count}");

        SetupPlayersAfterSceneLoad(sceneName);
    }

    private void SetupPlayersAfterSceneLoad(string sceneName)
    {
        foreach (var kv in PlayerManager.instance.playerMap)
        {
            var player = kv.Value;
            var obj = player.networkObject;

            var isWorld = sceneName == "Main World";
           
            if(!isWorld)
                PlayerManager.instance.ValidateSingleton();
            
            if (obj.TryGetComponent(out PlayerController controller))
            {
                if (isWorld)
                {
                    // Enable controller
                    controller.enabled = true;

                    // Only owner's camera
                    if (controller.IsOwner)
                        controller.camera.enabled = true;

                    
                    StartCoroutine(controller.WaitAndSpawn());
                }
                else
                {
                    // Disable in lobby
                    controller.enabled = false;
                    controller.CursorControll(false);
                    if (controller.camera != null)
                        controller.camera.enabled = false;
                }
            }

            if (obj.TryGetComponent(out WeaponHandler weapon))
            {
                weapon.enabled = isWorld;
            }

            if (obj.TryGetComponent(out PlayerDataLoader loader))
            {
                if (isWorld)
                    loader.LoadClassData(player.currentClass);
                else
                    loader.enabled = false; // disabled in lobby

                loader.playerName = kv.Key;
            }

            if (obj.TryGetComponent(out PlayerUI ui))
            {
                ui.SetPlayerName(kv.Key); 
            }
        }
    }


 

    
    #region GUI
    /*void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10,10,300,300));
        if(!IsClient && !IsServer)
        {
            StartButtons();
        }
        else
        {
            StatusLabels();
        }
        GUILayout.EndArea();

    }

    private void StartButtons()
    {
        if (GUILayout.Button("Host")) StartHost();
        if (GUILayout.Button("Client")) StartClient();
        if (GUILayout.Button("Server")) StartServer();
    }

    private void StatusLabels()
    {
        string mode = IsHost ? "Host" : IsServer ? "Server" : "Client";

        GUILayout.Label("Transport: " + NetworkConfig.NetworkTransport.GetType().Name);
        GUILayout.Label("Mode: " + mode); 
    }*/


  
    #endregion
}
