using System;
using Unity.Netcode;
using UnityEngine;

public class MainMenuManager : MonoBehaviour
{
    private FPSManager manager;

    private void Start()
    {
        manager = (FPSManager)(NetworkManager.Singleton);
    }

    public void StartButton()
    {
        manager.Startlobby();
    }

    public void JoinButton()
    {
        manager.JoinLobby();
    }
}
