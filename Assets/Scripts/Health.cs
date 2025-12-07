using Unity.Netcode;
using UnityEngine;

public class Health : NetworkBehaviour
{
    public const int MaxHealth = 100;
    private NetworkVariable<int> health = new(MaxHealth);

    public static Health operator ++(Health h)
    {
        h.health.Value++;
        return h;
    }
    
    public static Health operator --(Health h)
    {
        h.health.Value--;
        return h;
    }
    public static Health operator -(Health h,int ammount)
    {
        h.health.Value -= ammount;
        return h;
    }
    public static Health operator +(Health h,int ammount)
    {
        h.health.Value += ammount;
        return h;
    }
    public static implicit operator int(Health h)
    {
        return h.health.Value;
    }


} 
