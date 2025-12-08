using System;
using UnityEngine;

public class ClassMetaData : MonoBehaviour
{
    public static ClassMetaData instance;

    [Header("Class Data")] 
    [Header("Knight")] public GameObject knightModel;
    [Header("Ranger")] public GameObject rangerModel;
    [Header("Wizard")] public GameObject wizardModel;
    

    private void Start()
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

    public GameObject GetModelByClass(ClassSelector.ClassType type) => type switch
    {
            ClassSelector.ClassType.NoOne => null,
            ClassSelector.ClassType.Wizard => wizardModel,
            ClassSelector.ClassType.Knight => knightModel,
            ClassSelector.ClassType.Ranger => rangerModel,
            ClassSelector.ClassType.MaxClass => null,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
   

    
    
    
}
