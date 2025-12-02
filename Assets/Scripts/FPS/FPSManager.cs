using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;
using UnityEngine.SceneManagement;

public class FPSManager : NetworkManager
{
    
    public void StartGame()
    {
        //host and load the Main scene
        StartHost();
        
        //load next scene 
        
        LoadWorld();
    }

    public void JoinGame()
    {
        //TODO: List all hosted games

        StartClient();
        
       //LoadWorld(); 
    }

    public void LoadWorld()
    {
        
        var main = SceneManager.LoadScene("Main World",LoadSceneMode.Single);
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
