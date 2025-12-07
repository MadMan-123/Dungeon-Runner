using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class WeaponHandler : NetworkBehaviour
{
    public ClassSelector.ClassType type;
    private Camera cache;
    public override void OnNetworkSpawn()
    {
        cache = Camera.main;
        Debug.Log($"Weapon spawned. IsServer: {IsServer}, PoolManager exists: {PoolManager.Instance != null}");
        base.OnNetworkSpawn();
    } 
    
    private void Update()
    {
        if (!IsOwner) return;
        
        if (Input.GetButtonDown("Fire1"))
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
        throw new NotImplementedException();
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
    private void HandleProjectileAttackServerRPC()
    {
         
    }   
}
