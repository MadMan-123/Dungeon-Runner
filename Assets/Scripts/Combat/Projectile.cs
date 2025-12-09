using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


[RequireComponent(typeof(Rigidbody), typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    public float totalTime = 3f;
    public ProjectileType type;
    private float lifeTimer;
    private Core.NetworkGameObjectPool pool;
    private Vector3 velocity;
    public int damage = 10;
    private Rigidbody rb;
    private NetworkObject source;
    public enum ProjectileType
    {
        FireBall,
        Arrow,
        MaxProjectile
    }
    private void Awake()
    {
        rb = GetComponent<Rigidbody>(); 
    }

    public override void OnNetworkSpawn()
    {

        if (IsServer)
        {
            lifeTimer = totalTime;
        }
    }
  
    public void Init(NetworkObject firedSource,int newDamage,Vector3 pos, Vector3 vel, Core.NetworkGameObjectPool poolRef)
    {
        source = firedSource;
        pool = poolRef;
        lifeTimer = totalTime;
        velocity = vel;
        damage = newDamage;
        if (rb == null) rb = GetComponent<Rigidbody>();
        rb.isKinematic = false;
        // Set velocity to zero first
        rb.linearVelocity = Vector3.zero;
        // Apply velocity next to ensure position is set first
        rb.linearVelocity = velocity;
    }

    private void Update()
    {
       // Timer to despawn projectile 
        if(!IsServer) return;
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                DespawnProjectile();
            }    
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject == source.gameObject)
            return;
        //if(!IsServer) return;
        if (collision.gameObject.TryGetComponent(out Health health))
        {
            health -= damage;
        }
        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        rb.linearVelocity = Vector3.zero;
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            pool.ReturnObject(NetworkObject);
        }
        
    }
}
