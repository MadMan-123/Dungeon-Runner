using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WeaponHandler : NetworkBehaviour
{
    public ClassSelector.ClassType type;
    public Camera cache;
    public PoolManager poolManager;

    public bool canFire = false;
    public override void OnNetworkSpawn()
    {

        base.OnNetworkSpawn();
    }
    
    
    
    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetButtonDown("Fire1") && canFire)
        {
             StartAttack();
        }
    }

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

    private void RangerAttack()
    {
        throw new NotImplementedException();
    }

    private void KnightAttack()
    {
        HandleMeleeAttackServerRPC(10,cache.transform.position , cache.transform.forward);
    }

    private void WizardAttack()
    {
        
        HandleProjectileAttackServerRPC(
            10,
            50f,
            Projectile.ProjectileType.FireBall,
            cache.transform.position , 
            cache.transform.forward);
        
    }


    private Collider[] results = new Collider[20];
    [ServerRpc]
    private void HandleMeleeAttackServerRPC(int damage,Vector3 pos, Vector3 dir)
    {
        var radius = 0.5f;
        Array.Clear(results,0,results.Length);
        var count = Physics.OverlapSphereNonAlloc(pos + dir, radius,results);

        if (count > 0)
        {
            for (var i = 0; i < count; i++)
            {
                if (results[i].TryGetComponent(out Health health))
                {
                    health -= damage;
                }
            }
        }
    }
    
    [ServerRpc]
    private void HandleProjectileAttackServerRPC(int damage, float speed,Projectile.ProjectileType type, Vector3 pos, Vector3 dir)
    {
        if (!poolManager)
        {
            Debug.LogError("Cannot get the pool manager instance");
            return;
        }
        
        var key = type.ToString();

        var pool = poolManager.GetPool(key);
        if (pool == null)
        {
            Debug.Log($"Pool {key}, cannot be found");
            return;
        }

        var projectile = pool.GetObject();
        if(projectile == null) return;

  

        projectile.transform.position = pos;
        projectile.transform.forward = dir;

        if (projectile.TryGetComponent(out Projectile component))
        {
            component.Init(damage, pos, dir * speed, pool);
        }

        if (!projectile.IsSpawned)
            projectile.Spawn(true);


    }

    /*[ClientRpc]
    private void SetProjectilePosClientRPC(NetworkObject obj, Vector3 pos)
    {
        obj.transform.position = pos;
    }*/
}
