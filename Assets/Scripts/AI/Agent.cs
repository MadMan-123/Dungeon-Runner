using System;
using Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class Agent : NetworkBehaviour
{
    public struct Behaviour
    {
        public float weight;
        public Action function;
    }
    
    public enum Behaviours
    {
        Seek = 0,
        Flee,
        Arrival,
        ObstacleAvoid,
        Wander,
        Alignment,
        Cohesion,
        Separation,
        MaxBehaviours
    }

    public Behaviour[] CurrentBehaviours = new Behaviour[(int)Behaviours.MaxBehaviours];
    private NetworkGameObjectPool poolRef;
    private NavMeshAgent navAgent;
    private Health health;
    public void Init(Vector3 position, Quaternion rotation, Core.NetworkGameObjectPool pool)
    {
        if (!TryGetComponent(out navAgent))
        {
            Debug.LogError("No navmesh agent found");
        }

        if (!TryGetComponent(out health))
        {
            
            health.health.Value = 50;
        }
        transform.position = position;
        transform.rotation= rotation;
        poolRef = pool;
        
    }
    
    private void Start()
    {
         
    }

    private void Update()
    {
        navAgent.Move(transform.forward * Time.deltaTime);
    }

    public void OnDeath()
    {
        poolRef.ReturnObject(NetworkObject);
    }
    
}
