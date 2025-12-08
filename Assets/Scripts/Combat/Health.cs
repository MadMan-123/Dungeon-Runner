using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class Health : NetworkBehaviour
{
    public const int MaxHealth = 100;

    public NetworkVariable<int> health = new (
        MaxHealth,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    public UnityEvent<int,int> onDamage;
    public UnityEvent onDeath;
    
    
    public override void OnNetworkSpawn()
    {
        health.OnValueChanged += HandleDamage;
        base.OnNetworkSpawn();
    }

    void HandleDamage(int oldVal ,int newVal)
    {
        //this refers to this Health class meaning it will return the health.Value
        if (newVal <= 0 && oldVal > 0)
        {
            onDeath?.Invoke();
            return;
        }
        
        if (newVal < oldVal)
        {
            onDamage?.Invoke(oldVal, newVal);
        }
    }

    public static Health operator ++(Health h)
    {
        if (!h || !h.IsSpawned || !h.IsServer)
            return h;

        h.health.Value++;
        return h;
    }

    public static Health operator --(Health h)
    {
        if (!h || !h.IsSpawned || !h.IsServer)
            return h;

        h.health.Value--;
        return h;
    }

    public static Health operator -(Health h, int amount)
    {
        if (!h || !h.IsSpawned || !h.IsServer)
            return h;

        h.health.Value -= amount;
        return h;
    }

    public static Health operator +(Health h, int amount)
    {
        if (!h || !h.IsSpawned || !h.IsServer)
            return h;

        h.health.Value += amount;
        return h;
    }



} 
