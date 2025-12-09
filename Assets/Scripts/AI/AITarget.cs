using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class AITarget : NetworkBehaviour 
{
    public Type type;

    //Basic Descriptor of a target the AI will look for
    public int GetPriority() => type switch
    {
        Type.Enemy => 0,
        Type.Player => 1,
        _ => throw new ArgumentOutOfRangeException()
    };

    public enum Type 
    {
        Enemy,
        Player,
      
    }
    
}
