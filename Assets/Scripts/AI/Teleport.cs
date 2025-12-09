using System;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    //Simple teleport script to make sure falling players get caught 
    public Transform teleportSpot;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController player))
        {
            player.transform.position = teleportSpot.position;
        }
    }
}
