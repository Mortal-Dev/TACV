using Unity.Entities;
using UnityEngine;

public class FixedWingPrefabAuthoring : MonoBehaviour
{
    class Baking : Baker<PlayerEntitySpawnerAuthoring>
    {
        public override void Bake(PlayerEntitySpawnerAuthoring authoring)
        {
            //Entity prefabEntity = GetEntity(authoring.prefab, TransformUsageFlags.Dynamic);

            //Entity spawnerEntity = GetEntity(TransformUsageFlags.Dynamic);

           // AddComponent(spawnerEntity, new FixedWingPr() { prefab = prefabEntity });
        }
    }
}
