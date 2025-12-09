using System;
using System.Collections;
using System.Linq;
using Core;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataLoader : NetworkBehaviour
{
    public GameObject currentBody;
    public Camera currentCamera;
    public string playerName;
    
    // Different class models
 
    [Header("Knight")] public GameObject knightModel;
    [Header("Ranger")] public GameObject rangerModel;
    [Header("Wizard")] public GameObject wizardModel;

    private PlayerManager players;
    public void LoadClassData(ClassSelector.ClassType type)
    {
        //changing the colour 
        /*
         var bodyMat = renderer.material;
        var colour = type switch
        {
            ClassSelector.ClassType.NoOne => Color.ghostWhite,
            ClassSelector.ClassType.Knight => Color.red,
            ClassSelector.ClassType.Ranger => Color.darkGreen,
            ClassSelector.ClassType.Wizard => Color.purple
        };
        

        if (bodyMat)
            bodyMat.color = colour;
        */

        //ensures we definitely have the name as we can change and sync names using the prefabs 
        StartCoroutine(Delay(type));
    }

    private IEnumerator Delay(ClassSelector.ClassType type)
    {
        // Small delay so everything is loaded

        yield return new WaitForSeconds(0.15f);
          var model = ClassMetaData.instance.GetModelByClass(type);
          if (!model) yield break;
                
          // Grab the correct model info
                var name = model.name;

                // Pick which model to turn on
                var enabledModel = name switch
                {
                    "wizard" => wizardModel,
                    "ranger" => rangerModel,
                    "knight" => knightModel,
                    _ => throw new ArgumentOutOfRangeException()
                };

                // Pick which model to turn on
                enabledModel.SetActive(true);
                currentBody.SetActive(false);
                // If this player has a weapon handler, update it
 
                if (TryGetComponent(out WeaponHandler handler))
                {
                    handler.type = type;
                    handler.cache = currentCamera;
                    handler.poolManager = PoolManager.Instance;
                    handler.canFire = true;
                }

       

    }

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerServerRPC(string playerName, ServerRpcParams rpcParams = default)
    {
        if (players == null) players = PlayerManager.instance;
        // If they aren't in the list, stop

        if (!players.HasPlayer(playerName))
            return;

        // Remove them on the server
        players.RemovePlayer(playerName);
        players.currentPlayers.Value--;

        // Tell every client to update too
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
        // Remove them locally if they exist

        if (players.HasPlayer(playerName))
            players.RemovePlayer(playerName);

    }


    public void HandleDeath()
    {
        RemovePlayerServerRPC(playerName);
        NetworkManager.Singleton.Shutdown();
        Destroy(players.gameObject);
        Loader.Load(Loader.Scene.MainMenu);
        
    }
}
