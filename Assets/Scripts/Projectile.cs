using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D), typeof(NetworkObject))]
public class Projectile : NetworkBehaviour
{
    private Rigidbody2D rb;
    private float ttl = 3f;
    private float lifeTimer;
    private Core.NetworkGameObjectPool pool;
    private Vector2 velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>(); // Get it in Awake, not OnNetworkSpawn
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            lifeTimer = ttl;
        }
    }

    public void Init(Vector3 pos, Vector2 vel, Core.NetworkGameObjectPool poolRef)
    {
        if (!IsServer) return;

        pool = poolRef;
        lifeTimer = ttl;
        velocity = vel;

        if (rb == null) rb = GetComponent<Rigidbody2D>();
    
        //Set velocity to zero first, then position, then apply new velocity
        rb.linearVelocity = Vector2.zero;
        rb.angularVelocity = 0f;
        transform.position = pos;
    
        //Apply velocity in next physics frame to ensure position is set first
        rb.linearVelocity = velocity;
    
        Debug.Log($"Projectile initialized at {pos} with velocity {vel}, rb.velocity is now {rb.linearVelocity}");
    }






    private void Update()
    {
        if (IsServer)
        {
            lifeTimer -= Time.deltaTime;
            if (lifeTimer <= 0f)
            {
                DespawnProjectile();
            }    
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsServer) return;
        DespawnProjectile();
    }

    private void DespawnProjectile()
    {
        rb.linearVelocity = Vector2.zero;
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            pool.ReturnObject(NetworkObject);
        }
    }
}
