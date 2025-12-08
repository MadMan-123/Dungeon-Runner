using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerDataLoader : NetworkBehaviour
{
    public GameObject currentBody;
    public Camera currentCamera;
    [Header("Knight")] public GameObject knightModel;
    [Header("Ranger")] public GameObject rangerModel;
    [Header("Wizard")] public GameObject wizardModel;
    public void LoadClassData(ClassSelector.ClassType type)
    {
        //changing the colour 
        /*
         var bodyMat = renderer.material;
        var colour = type switch
        {
            ClassSelector.ClassType.NoOne => Color.ghostWhite,
            ClassSelector.ClassType.Knight => Color.red,
            ClassSelector.ClassType.Ranger => Color.darkGreen,
            ClassSelector.ClassType.Wizard => Color.purple
        };
        

        if (bodyMat)
            bodyMat.color = colour;
        */

        //ensures we definitely have the name as we can change and sync names using the prefabs 
        StartCoroutine(Delay(type));
    }

    private IEnumerator Delay(ClassSelector.ClassType type)
    {
        yield return new WaitForSeconds(0.15f);
          var model = ClassMetaData.instance.GetModelByClass(type);
          if (!model) yield return null;
                
                var name = model.name;
        
                var enabledModel = name switch
                {
                    "wizard" => wizardModel,
                    "ranger" => rangerModel,
                    "knight" => knightModel,
                    _ => throw new ArgumentOutOfRangeException()
                };
        
                enabledModel.SetActive(true);
                currentBody.SetActive(false);
                
                if (TryGetComponent(out WeaponHandler handler))
                {
                    handler.type = type;
                    handler.cache = currentCamera;
                    handler.poolManager = PoolManager.Instance;
                    handler.canFire = true;
                }

    }
}
