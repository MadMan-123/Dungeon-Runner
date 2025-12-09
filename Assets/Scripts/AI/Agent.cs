using System;
using Core;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class Agent : NetworkBehaviour
{
    // Hold a function with a weight in AOS format
    public struct Behaviour
    {
        public float weight;
        public Func<Vector3,Vector3> function;
    }
    
    // Basic Steering behaviours 
    // Note that not all of the behaviours are implemented yet (are just replaced by seek),
    // i can later work on the AI to make it alot more fun but in the context of this project right now i just need to prove i can have an agent spawn and perform the steering behaviours whilst synced
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

    // Finite state machine states which dictate how the steering behaviours are controlled
    public enum AgentState
    {
        Wander,
        Chase,
        Attack,
        Flee
    }
    
    // Detection parameters
    [SerializeField] private float detectRadius = 10f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float lowHealthThreshold = 15f;

    //Agent reference  
    public NavMeshAgent navAgent;
    // Actual FSM state
    public AgentState currentState = AgentState.Wander;

    // World reader reference
    public WorldReader reader;
    // Array of behaviours
    public Behaviour[] CurrentBehaviours ;
    public float MaxCurrentSpeed = 5;
    // Pool reference
    private NetworkGameObjectPool poolRef;
    private Health health;
    
    // Steering behaviour data
    [Header("AI Data")]
    private float circleDistance = 5;
    private float circleRadius = 4;
    private float rotationSpeed = 3;
    private float lastAttackTime;
    private float attackCooldown = 1f;
    private float attackDamage = 10f;
    private float wanderAngle = 0;
    private Vector3 steeringVelocity = new();
    private Vector3 currentVelocity = new();

    private void Start()
    {
        //make sure we have a world reader
        if (reader == null)
        {
            reader = GetComponent<WorldReader>();
            if (reader == null)
            {
                Debug.LogError("[Agent] Missing WorldReader reference");
            }
        }
       
        //Setup all steering behaviours

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
        
        // Fill in the behaviours
        CurrentBehaviours = new[] {seek,flee,arrival,obstacle,wander,alignment,cohesion,separation};

        // Manually update the rotation 
        navAgent.updateRotation = false;

    }
    

    public void Init(Vector3 position, Quaternion rotation, Core.NetworkGameObjectPool pool)
    {
        //make sure we have a navmesh agent
        if (!TryGetComponent(out navAgent))
        {
            Debug.LogError("No navmesh agent found");
        }

        if (!IsServer)
        {
            // Disable if not server
            navAgent.enabled = false;
        }
        // Setup health 
        if (!TryGetComponent(out health))
        {
            //Ugly i know 
            health.health.Value = 50;
        }
        // Setup agent transform
        transform.position = position;
        transform.rotation= rotation;
        // Link pool
        poolRef = pool;
        
    }
    
    protected virtual void CooperativeArbitration()
    {
        // Set to zero
        steeringVelocity = Vector3.zero;
        // Get target from reader
        var target = reader.ClosestTarget;
        var endPoint = Vector3.zero;
        
        // Check if its valid
        if(target)
           endPoint = target.transform.position;
        
        // Sum all behaviour output 
        for (var index = 0; index < CurrentBehaviours.Length; index++)
        {
            var currentBehaviour = CurrentBehaviours[index];
            //Call the function and times its output by its weight
            steeringVelocity += currentBehaviour.function(endPoint) * currentBehaviour.weight;
        }
    } 
    private void Update()
    {
        // Only server should update
        if(!IsServer)
            return;

        // Ensure we have a reader to do anything
        if (reader == null)
            return;

        //Evaluate what FSM state to be
        EvaluateState();
        // Apply the weights
        ApplyStateWeights();
        // Apply the behaviours velocities
        CooperativeArbitration();
        
        
        //Start Agent attack if we can
        if (currentState == AgentState.Attack)
        {
            PerformAttack();
        }
        // Move the agent by the summer steering velocity
        navAgent.Move(steeringVelocity * Time.deltaTime);

        // Check if we need to rotate
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


    // Handle death
    public void OnDeath()
    {
        poolRef.ReturnObject(NetworkObject);
    }
    
    #region Behaviours

    Vector3 Seek(Vector3 target)
    {
        // the direction to the target pos
        return (target - transform.position).normalized * MaxCurrentSpeed; 
    }
    
    
    private Vector3 Wander(Vector3 target)
    {
        int randDifference = 5;
        wanderAngle += Random.Range(-randDifference, randDifference) * Mathf.Deg2Rad;
        var circlePos = transform.position + (transform.forward * circleDistance);
    
        // Work out a random point on circle then get a direction to that and set as steering velocity
        var offsetX = Mathf.Cos(wanderAngle) * circleRadius;
        var offsetZ = Mathf.Sin(wanderAngle) * circleRadius;

        var targetPos = new Vector3(
            circlePos.x + offsetX,
            transform.position.y,
            circlePos.z + offsetZ);
            
        //Seek
        return (targetPos - transform.position).normalized * MaxCurrentSpeed;
    }
    private Vector3 Idle()
    {
        // Return nothing
        return Vector3.zero;
    }
    
    private Vector3 AvoidObstacles(Vector3 target)
    {
        // Judge if there is something infront of the AI or they are looking off the edge of the navmesh
        if (Physics.Raycast(transform.position, transform.forward, 1f) ||
            !NavMesh.SamplePosition(transform.position + transform.forward, out var hit, 0.5f, NavMesh.AllAreas))
        {
            // Turn all the way around by 180 degrees
            transform.Rotate(0, 180, 0);
            //Draw the new direction and the ray to the obstacle or edge
            Debug.DrawRay(transform.position, transform.forward, Color.red, 0.1f);
        }
        return Seek(navAgent.destination);

    }

    private Vector3 Flee(Vector3 target)
    {
        // Find a point that is opposite to the target
        return (transform.position - target).normalized * MaxCurrentSpeed;
    } 
    
    #endregion
    private void PerformAttack()
    {
        // Only server can call this
        if (!IsServer) return;

        // Get target
        var target = reader.ClosestTarget;
        
        if (!target) return;

        // Get health
        if (!target.TryGetComponent(out Health targetHealth)) return;
        if (targetHealth.health.Value <= 0) return;

        // Cooldown
        if (Time.time - lastAttackTime < attackCooldown)
            return;

        lastAttackTime = Time.time;

        // Damage
        targetHealth -= (int)attackDamage;
    }

    private void EvaluateState()
    {
        // Get closest target
        var target = reader.ClosestTarget;
        // Get health
        float hp = health.health.Value;

        // Do we have health?
        bool hasTarget = target != null;
        float dist = reader.closestDistance;
        
        //TODO: use utility theory to evaluate weights based on total players health and total agents health compared to relevant counts
        
        // Judge state based on health
        if (hp <= lowHealthThreshold)
        {
            currentState = AgentState.Flee;
            return;
        }

        // Evaluate state based on distance
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
                if (hp > lowHealthThreshold * 1.5f) 
                    currentState = AgentState.Wander;
                break;
        }
    }
    private void ApplyStateWeights()
    {
        // Reset all
        for (int i = 0; i < CurrentBehaviours.Length; i++)
            CurrentBehaviours[i].weight = 0;

        // Set weights based on state
        //TODO: Here is where we need utility theory
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
