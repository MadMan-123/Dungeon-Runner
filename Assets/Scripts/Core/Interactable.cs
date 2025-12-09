using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Interactable : NetworkBehaviour
{
    public UnityEvent<NetworkObject> onInteract;
    public void Interact(NetworkObject interactor)
    {
        onInteract?.Invoke(interactor);
    }

    private void Start()
    {
        //set layer to interactable
        gameObject.layer = LayerMask.NameToLayer("Interactable");
    }
}
