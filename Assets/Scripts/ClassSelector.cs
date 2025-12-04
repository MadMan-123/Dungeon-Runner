using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ClassSelector : MonoBehaviour
{
    
    public static ClassSelector instance; 
    public enum ClassType
    {
        NoOne = -1,
        Wizard,
        Knight,
        Ranger,
        MaxClass
    }


    private void Awake()
    {
        if (!instance)
        {
            instance = this;
        }
        else
        {
            Destroy(instance);
        }
    }

    public ClassType currentType = ClassType.NoOne;

   
    
    public void SetType(int index)
    {
        if(index is > (int)ClassType.MaxClass or < 0)
            return;
        
        currentType = (ClassType)index;
       
        
    }




}
