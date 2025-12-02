using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.Serialization;


public class MultiplayerManager : NetworkManager
{
    
    public int maxPlayers = 4;
    public int currentPlayers = 0;
    void OnGUI()
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
    }
    
    
    
}
