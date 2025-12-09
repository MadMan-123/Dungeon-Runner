using System;
using UnityEngine;

public class Teleport : MonoBehaviour
{
    public Transform teleportSpot;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out PlayerController player))
        {
            player.transform.position = teleportSpot.position;
        }
    }
}
