using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class BulletAuthoring : MonoBehaviour
{
    public class Baker : Baker<BulletAuthoring>
    {
        public override void Bake(BulletAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Bullet { 
                timer = 5f,
                vel = 0,
            });
        }
    }
}

public struct Bullet : IComponentData , IEnableableComponent
{
    public float timer;
    public float3 vel;
}
