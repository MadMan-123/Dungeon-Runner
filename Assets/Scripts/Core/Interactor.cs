using System;
using Unity.Netcode;
using UnityEngine;

public class Interactor : NetworkBehaviour
{
    public float interactRange = 2f;
    public LayerMask interactMask;
    public LayerMask ignoreMask;
    public Camera playerCamera;

    private void Update()
    {
        if (!IsOwner) return;

        // Always draw ray so you can see it in Scene view
        Debug.DrawRay(playerCamera.transform.position,
            playerCamera.transform.forward * interactRange,
            Color.yellow);

        if (Input.GetKeyDown(KeyCode.E))
        {
            TryInteractRaycast();
        }
    }

    private void TryInteractRaycast()
    {
        // Combine masks by removing ignored layers
        int mask = interactMask & ~ignoreMask;


        if (Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                interactRange,
                mask))
        {
            Debug.Log($"[Interactor] Raycast hit: {hit.collider.name}");

            if (hit.collider.TryGetComponent(out Interactable interactable))
            {
                StartInteractionServerRpc(interactable.NetworkObject.NetworkObjectId);
            }

        }

    }

    [ServerRpc(RequireOwnership = false)]
    private void StartInteractionServerRpc(ulong targetObjectId)
    {
        Debug.Log($"[Interactor][Server] Received interaction request: netObjId={targetObjectId}");

        if (!NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(targetObjectId, out NetworkObject netObj))
        {
            Debug.Log("[Interactor][Server] NetworkObject NOT FOUND.");
            return;
        }

        if (!netObj.TryGetComponent(out Interactable interactable))
        {
            Debug.Log("[Interactor][Server] Object has no Interactable component.");
            return;
        }

        interactable.Interact(NetworkObject);
    }
}
