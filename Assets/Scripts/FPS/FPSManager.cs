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
    private void OnSceneLoadedForAll(string sceneName, LoadSceneMode mode,List<ulong> clientsComplete, List<ulong> clientsTimedOut)
    {
        if (sceneName != "Main World")
            return;
        Debug.Log($"[FPSManager] Scene load completed for '{sceneName}'. " +
                  $"IsServer={IsServer}, IsHost={IsHost}, IsClient={IsClient}. " +
                  $"Clients complete: {clientsComplete.Count}, timed out: {clientsTimedOut.Count}");
        SetupPlayersAfterSceneLoad();
    }
    private void SetupPlayersAfterSceneLoad()
    {
        foreach (var kv in PlayerManager.instance.playerMap)
        {
            var player = kv.Value; 
            var obj = player.networkObject;
            
            if (obj != null && obj.TryGetComponent(out PlayerController ctrl))
            {
                if (ctrl.camera != null && ctrl.IsOwner)
                {
                    ctrl.camera.enabled = true;
                }
                StartCoroutine(ctrl.WaitAndSpawn());
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
