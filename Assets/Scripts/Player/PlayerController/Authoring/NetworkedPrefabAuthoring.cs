using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class NetworkedPrefabAuthoring : MonoBehaviour
{
    class Baking : Baker<NetworkedPrefabAuthoring>
    {
        public override void Bake(NetworkedPrefabAuthoring authoring)
        {
            SpawnerAuthoringBase spawnerAuthoringBase = authoring.gameObject.GetComponent<SpawnerAuthoringBase>();

            AddComponent(GetEntity(TransformUsageFlags.Dynamic), new NetworkedPrefabComponent() { prefab = GetEntity(spawnerAuthoringBase.prefab, TransformUsageFlags.Dynamic) });
        }
    }
}