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


    private Collider[] hits = new Collider[64]; // non-alloc buffer
    private List<Vector3> nearbyTargetsPos = new(10);
    private List<Vector3> nearbyAllyPos = new(25);

    private void Update()
    {
        //do a circle cast
        int count = Physics.OverlapSphereNonAlloc(
            transform.position, radius, hits);

        //Set data to default values
        avgDistance = 0f;
        nearbyTargets.Clear();
        nearbyAllies.Clear();
        nearbyTargetsPos.Clear();
        nearbyAllyPos.Clear();
 
        
        var allyCount = 0;
        var playerCount = 0;
        Count = count; 
        //get all the agents
        for (int i = 0; i < count; i++)
        {
            
           /*if(!FOVCheck(hits[i].point))
               continue;*/
            
            //check if the target is an AITarget
            if (!hits[i].GetComponent<Collider>().TryGetComponent(out AITarget target)) continue;
            
            
            //check if the target is an ally we want to consider
            if(target.gameObject.TryGetComponent(out PlayerController player))
            {
                    //add the agent to the list
                    nearbyTargets.Add(target.NetworkObject);
                    //increment the count
                    playerCount++;
                    //add the position to the list
                    var position = player.transform.position;
                    //add the position to the list
                    nearbyTargetsPos.Add(position);
                    //add the distance to the avg distance
                    avgDistance += (position - transform.position).magnitude;
                
            }
            if(target.TryGetComponent(out Agent agent))
            {
                //add the agent to the list
                nearbyAllies.Add(agent);
                //increment the count
                allyCount++;
                var pos = target.transform.position;
                nearbyAllyPos.Add(pos);
            }
        }
        
        
        if(count > 0)
            //normalize the avg distance
            avgDistance /= playerCount;
        else
            //set the avg distance to 0
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
