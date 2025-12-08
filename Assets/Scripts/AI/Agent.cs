using System;
using Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Agent : NetworkBehaviour
{
    public struct Behaviour
    {
        public float weight;
        public Func<Vector3,Vector3> function;
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

    public enum AgentState
    {
        Wander,
        Chase,
        Attack,
        Flee
    }
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float lowHealthThreshold = 15f;

    private AgentState currentState = AgentState.Wander;

    public WorldReader reader;
    public Behaviour[] CurrentBehaviours ;
    public float MaxCurrentSpeed = 5;
    private NetworkGameObjectPool poolRef;
    private NavMeshAgent navAgent;
    private Health health;
    private float wanderAngle = 0;
    private Vector3 steeringVelocity = new();
    private Vector3 currentVelocity = new();
    
    [Header("AI Data")]
    private float circleDistance = 5;
    private float circleRadius = 4;
    private float rotationSpeed = 3;
    private float lastAttackTime;
    private float attackCooldown = 1f;
    private float attackDamage = 10f;

    private void Start()
    {
        Behaviour seek;
        seek.function = Seek;
        seek.weight = 0;
        Behaviour flee;
        flee.function = Flee;
        flee.weight = 0;
        Behaviour arrival;
        arrival.function = Seek;
        arrival.weight = 0;
        Behaviour obstacle;
        obstacle.function = AvoidObstacles;
        obstacle.weight = 0;
        Behaviour wander;
        wander.function = Wander;
        wander.weight = 1;
        
        //TODO: Flocking, for now all just seek
        Behaviour alignment;
        alignment.function = Seek;
        alignment.weight = 0;
        Behaviour cohesion;
        cohesion.function = Seek;
        cohesion.weight = 0;
        Behaviour separation;
        separation.function = Seek;
        separation.weight = 0;
        
        
        //fill in the behaviours
        CurrentBehaviours = new[] {seek,flee,arrival,obstacle,wander,alignment,cohesion,separation};

        navAgent.updateRotation = false;

    }
    
    

    public void Init(Vector3 position, Quaternion rotation, Core.NetworkGameObjectPool pool)
    {
        if (!TryGetComponent(out navAgent))
        {
            Debug.LogError("No navmesh agent found");
        }

        if (!IsServer)
        {
            navAgent.enabled = false;
        }
        if (!TryGetComponent(out health))
        {
            
            health.health.Value = 50;
        }
        transform.position = position;
        transform.rotation= rotation;
        poolRef = pool;
        
    }
    protected virtual void CooperativeArbitration()
    {
        steeringVelocity = Vector3.zero;
        var target = reader.ClosestTarget;
        var endPoint = Vector3.zero;
        if(target)
           endPoint = target.transform.position; 
       
        for (var index = 0; index < CurrentBehaviours.Length; index++)
        {
            var currentBehaviour = CurrentBehaviours[index];
            steeringVelocity += currentBehaviour.function(endPoint) * currentBehaviour.weight;
        }
    } 
    private void Update()
    {
        if(!IsServer)
            return;
        EvaluateState();
        ApplyStateWeights();

        CooperativeArbitration();

        if (currentState == AgentState.Attack)
        {
            PerformAttack();
        }
        navAgent.Move(steeringVelocity * Time.deltaTime);

        if (steeringVelocity.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(steeringVelocity, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                rotationSpeed * Time.deltaTime
            );
        }
    }


    public void OnDeath()
    {
        poolRef.ReturnObject(NetworkObject);
    }

    #region Behaviours

    Vector3 Seek(Vector3 target)
    {
        return (target - transform.position).normalized * MaxCurrentSpeed; 
    }
    
    private Vector3 Wander(Vector3 target)
    {
        int randDifference = 5;
        wanderAngle += Random.Range(-randDifference, randDifference) * Mathf.Deg2Rad;
        var circlePos = transform.position + (transform.forward * circleDistance);
            
        var offsetX = Mathf.Cos(wanderAngle) * circleRadius;
        var offsetZ = Mathf.Sin(wanderAngle) * circleRadius;

        var targetPos = new Vector3(
            circlePos.x + offsetX,
            transform.position.y,
            circlePos.z + offsetZ);
            
        return (transform.position - targetPos).normalized * MaxCurrentSpeed;
    }
    private Vector3 Idle()
    {
        return Vector3.zero;
    }
    
    private Vector3 AvoidObstacles(Vector3 target)
    {
        //judge if there is something infront of the AI or they are looking off the edge of the navmesh
        if (Physics.Raycast(transform.position, transform.forward, 1f) ||
            !NavMesh.SamplePosition(transform.position + transform.forward, out var hit, 0.5f, NavMesh.AllAreas))
        {
            //turn all the way around by 180 degrees
            transform.Rotate(0, 180, 0);
            //draw the new direction and the ray to the obstacle or edge
            Debug.DrawRay(transform.position, transform.forward, Color.red, 0.1f);
        }
        return Seek(navAgent.destination);

    }

    private Vector3 Flee(Vector3 target)
    {
             
        //find a point that is opposite to the target
        return (transform.position - target).normalized * MaxCurrentSpeed;
    
    } 
    
    #endregion
    private void PerformAttack()
    {
        if (!IsServer) return;

        var target = reader.ClosestTarget;
        if (!target) return;

        if (!target.TryGetComponent(out Health targetHealth)) return;
        if (targetHealth.health.Value <= 0) return;

        // Cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        
        targetHealth -= (int)attackDamage;
    }

    private void EvaluateState()
    {
        var target = reader.ClosestTarget;
        float hp = health.health.Value;

        bool hasTarget = target != null;
        float dist = reader.closestDistance;

        if (hp <= lowHealthThreshold)
        {
            currentState = AgentState.Flee;
            return;
        }

        switch (currentState)
        {
            case AgentState.Wander:
                if (hasTarget && dist <= detectRadius)
                    currentState = AgentState.Chase;
                break;

            case AgentState.Chase:
                if (!hasTarget)
                    currentState = AgentState.Wander;
                else if (dist <= attackRange)
                    currentState = AgentState.Attack;
                break;

            case AgentState.Attack:
                if (!hasTarget)
                    currentState = AgentState.Wander;
                else if (dist > attackRange)
                    currentState = AgentState.Chase;
                break;

            case AgentState.Flee:
                if (hp > lowHealthThreshold * 1.5f) // recovered
                    currentState = AgentState.Wander;
                break;
        }
    }
    private void ApplyStateWeights()
    {
        // Reset all
        for (int i = 0; i < CurrentBehaviours.Length; i++)
            CurrentBehaviours[i].weight = 0;

        switch (currentState)
        {
            case AgentState.Wander:
                CurrentBehaviours[(int)Behaviours.Wander].weight = 1;
                CurrentBehaviours[(int)Behaviours.ObstacleAvoid].weight = 0.5f;
                break;

            case AgentState.Chase:
                CurrentBehaviours[(int)Behaviours.Seek].weight = 1;
                CurrentBehaviours[(int)Behaviours.ObstacleAvoid].weight = 0.7f;
                break;

            case AgentState.Attack:
                CurrentBehaviours[(int)Behaviours.Arrival].weight = 1;
                break;

            case AgentState.Flee:
                CurrentBehaviours[(int)Behaviours.Flee].weight = 1;
                CurrentBehaviours[(int)Behaviours.ObstacleAvoid].weight = 0.7f;
                break;
        }
    }

    private void OnDrawGizmos()
    {   
        
    }
}
