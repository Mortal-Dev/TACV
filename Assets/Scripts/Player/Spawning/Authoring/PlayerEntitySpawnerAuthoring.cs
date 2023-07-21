using UnityEngine;
using Unity.Entities;

public class PlayerEntitySpawnerAuthoring : SpawnerAuthoringBase
{
    class Baking : Baker<PlayerEntitySpawnerAuthoring>
    {
        public override void Bake(PlayerEntitySpawnerAuthoring authoring)
        {
            Entity prefabEntity = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);

            Entity spawnerEntity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(spawnerEntity, new PlayerPrefabComponent() { prefab = prefabEntity });
        }
    }
}