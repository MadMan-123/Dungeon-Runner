
using Unity.Entities;
using UnityEngine;

public class AgentAuthoring : MonoBehaviour
{
    public class Baker : Baker<AgentAuthoring>
    {
        public override void Bake(AgentAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Agent());
        }
    }
}


public struct Agent : IComponentData
{

}