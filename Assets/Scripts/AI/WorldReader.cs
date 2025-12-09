using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class WorldReader : NetworkBehaviour 
{
    public List<NetworkObject> nearbyTargets = new(20);
    public List<Agent> nearbyAllies= new (25);
    public float radius = 15f;
    public float FOV = 90f;
    public int Count = 0;
    private bool shouldDebug = false;
    public float avgDistance = 0f;
    public LayerMask detectionMask = -1; 

    public Collider[] results = new Collider[32]; 
    private List<Vector3> nearbyTargetsPos = new(10);
    private List<Vector3> nearbyAllyPos = new(25);

    public NetworkObject ClosestTarget;
    public float closestDistance = 0;
    private void Update()
    {
        if(!IsServer)
            return;
        
        // Clear the results array to remove stale references
        Array.Clear(results, 0, results.Length);
        
        //do a circle cast
        var size = Physics.OverlapSphereNonAlloc(transform.position, radius, results, detectionMask);

        //Set data to default values
        avgDistance = 0f;
        nearbyTargets.Clear();
        nearbyAllies.Clear();
        nearbyTargetsPos.Clear();
        nearbyAllyPos.Clear();
        closestDistance = float.PositiveInfinity;
        ClosestTarget = null;
 
        
        var allyCount = 0;
        var playerCount = 0;
        Count = size; 
        //get all the agents
        
        for (int i = 0; i < Count; i++)
        {
            
            // Skip null or invalid colliders
            if (results[i] == null || results[i].gameObject == null)
                continue;
            
            if (results[i].transform.root == transform.root)
                continue; 

                       
            if(!FOVCheck(results[i].transform.position))
                continue;
            
            // Check if the target is an AITarget
            if (!results[i].TryGetComponent(out AITarget target)) continue;
            
            
            // Check if the target is an ally we want to consider
            if(target.gameObject.TryGetComponent(out PlayerController player))
            {
                  
                        //Add the agent to the list
                        nearbyTargets.Add(target.NetworkObject);
                        //Increment the count
                        playerCount++;
                        //Add the position to the list
                        var position = player.transform.position;
                        //Add the position to the list
                        nearbyTargetsPos.Add(position);
                        //Add the distance to the avg distance
                        var distance = (position - transform.position).magnitude;
                        avgDistance += distance;
                        //Set closest target
                        if (distance < closestDistance)
                        {
                            ClosestTarget = target.NetworkObject;
                            closestDistance = distance;
                        }

                        continue;
                
            }
            
            if(target.TryGetComponent(out Agent agent))
            {
                if (agent != null && agent.enabled && agent.gameObject.activeInHierarchy)
                {
                    nearbyAllies.Add(agent);
                    allyCount++;
                    var pos = target.transform.position;
                    nearbyAllyPos.Add(pos);
                }
            }
            
            
        }
        
        
        if(playerCount > 0)
            // Normalize the avg distance
            avgDistance /= playerCount;
        else
            // Set the avg distance to 0
            avgDistance = 0f;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.aquamarine;
        //draw the radius
        Gizmos.DrawWireSphere(transform.position,radius);
        
    }
    
    public bool FOVCheck(Vector3 target)
    {
        Vector3 toTarget = (target - transform.position).normalized;

        float angle = Vector3.Angle(transform.forward, toTarget);

        return angle <= FOV * 0.5f;
    }

    
}
