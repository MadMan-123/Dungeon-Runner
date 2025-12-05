using Unity.Netcode;
using UnityEngine;

public class PlayerDataLoader : NetworkBehaviour
{
    [SerializeField] private MeshRenderer renderer;



    public void LoadClassData(ClassSelector.ClassType type)
    {
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
    }
}
