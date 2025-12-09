using System;
using Unity.Netcode;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    // Player's class (wizard, knight, etc.)
    public ClassSelector.ClassType type;

    // Camera we shoot from
    public Camera cache;

    // Access to projectile pools
    public PoolManager poolManager;

    // Stops the player firing until their class/model is ready
    public bool canFire = false;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
    }

    private void Update()
    {
        // Only the owning client can fire
        if (!IsOwner) return;

        // Left click
        if (Input.GetButtonDown("Fire1") && canFire)
        {
            StartAttack();
        }
    }

    // Chooses which attack to run based on class
    public void StartAttack()
    {
        switch (type)
        {
            case ClassSelector.ClassType.NoOne:
                break;

            case ClassSelector.ClassType.Wizard:
                WizardAttack();
                break;

            case ClassSelector.ClassType.Knight:
                KnightAttack();
                break;

            case ClassSelector.ClassType.Ranger:
                RangerAttack();
                break;

            case ClassSelector.ClassType.MaxClass:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    // Ranger = shoots an arrow
    private void RangerAttack()
    {
        HandleProjectileAttackServerRPC(
            10,                 // damage
            50f,                // speed
            Projectile.ProjectileType.Arrow,
            cache.transform.position,
            cache.transform.forward
        );
    }

    // Knight = basic melee hit
    private void KnightAttack()
    {
        HandleMeleeAttackServerRPC(
            10,
            cache.transform.position,
            cache.transform.forward
        );
    }

    // Wizard = shoots fireball
    private void WizardAttack()
    {
        HandleProjectileAttackServerRPC(
            10,
            50f,
            Projectile.ProjectileType.FireBall,
            cache.transform.position,
            cache.transform.forward
        );
    }

    private Collider[] results = new Collider[20];

    // Server handles melee hit detection
    [ServerRpc]
    private void HandleMeleeAttackServerRPC(int damage, Vector3 pos, Vector3 dir)
    {
        var radius = 0.5f;

        // Clear old results
        Array.Clear(results, 0, results.Length);

        // Small sphere in front of the player
        var count = Physics.OverlapSphereNonAlloc(pos + dir, radius, results);

        // Hit anything?
        if (count > 0)
        {
            for (var i = 0; i < count; i++)
            {
                // If it has health, damage it
                if (results[i].TryGetComponent(out Health health))
                {
                    health -= damage;
                }
            }
        }
    }

    // Server spawns and launches projectiles
    [ServerRpc]
    private void HandleProjectileAttackServerRPC(int damage, float speed, Projectile.ProjectileType type, Vector3 pos, Vector3 dir)
    {
        // Make sure the pool manager exists
        if (!poolManager)
        {
            Debug.LogError("Cannot get the data manager Instance");
            return;
        }

        var key = type.ToString();

        // Get the pool for this projectile type
        var pool = poolManager.GetPool(key);
        if (pool.data == null)
        {
            Debug.Log($"Pool {key} cannot be found");
            return;
        }

        // Grab a projectile from the pool
        var projectile = pool.data.GetObject();
        if (projectile == null) return;

        // Set spawn position + direction
        projectile.transform.position = pos;
        projectile.transform.forward = dir;

        // Setup projectile logic
        if (projectile.TryGetComponent(out Projectile component))
        {
            component.Init(NetworkObject, damage, pos, dir * speed, pool.data);
        }

        // Spawn on network if not already spawned
        if (!projectile.IsSpawned)
            projectile.Spawn(true);

        // Parent it under the pool
        projectile.transform.SetParent(pool.parent.transform);
    }
}
