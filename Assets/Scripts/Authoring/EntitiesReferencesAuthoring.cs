using Unity.Entities;
using UnityEngine;

public class EntitiesReferencesAuthoring : MonoBehaviour
{
    public GameObject playerPrefabGameObject;
    public GameObject bulletPrefabGameObject;
    public GameObject agentPrefabGameObject;
    public class Baker : Baker<EntitiesReferencesAuthoring>
    {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            Entity playerPrefab = GetEntity(authoring.playerPrefabGameObject, TransformUsageFlags.Dynamic);
            Entity bulletPrefab = GetEntity(authoring.bulletPrefabGameObject, TransformUsageFlags.Dynamic);
            Entity agentPrefab = GetEntity(authoring.agentPrefabGameObject, TransformUsageFlags.Dynamic);


            AddComponent(entity, new EntitiesReferences
            {
               PlayerPrefabEntity = playerPrefab,
               BulletPrefabEntity = bulletPrefab,
               AgentPrefabEntity = agentPrefab,
            });
        }
    }
}

public struct EntitiesReferences : IComponentData
{
    public Entity PlayerPrefabEntity;
    public Entity BulletPrefabEntity;
    public Entity AgentPrefabEntity;
}