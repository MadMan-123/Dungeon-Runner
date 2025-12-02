using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ScoreItem : NetworkBehaviour
{
    public int scoreToAdd = 1;
    public ScoreManager manager;

    public override void OnNetworkSpawn()
    { 
        manager = GetComponentInParent<ScoreManager>();
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerMetaData data;
            if(other.gameObject.TryGetComponent(out data))
            {
                Debug.Log($"Player {data.playerId} collided with score item");
                manager?.AddScoreServerRPC((ulong)data.playerId,scoreToAdd);
                
            }
            //add score to manager
        }
    }
}
