using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class NetworkedPrefabsAuthoring : MonoBehaviour
{
    public List<GameObject> NetworkedPrefabs;

    class Baking : Baker<NetworkedPrefabsAuthoring>
    {
        public override void Bake(NetworkedPrefabsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            NetworkedPrefabsComponent networkedPrefabsComponent = new NetworkedPrefabsComponent() { NetworkedPrefabs = new NativeHashMap<int, Entity>() };

            foreach (GameObject gameObject in authoring.NetworkedPrefabs)
            {
                networkedPrefabsComponent.NetworkedPrefabs.Add(gameObject.name.GetHashCode(), GetEntity(gameObject, TransformUsageFlags.Dynamic));
            }

            AddComponent(entity, networkedPrefabsComponent);
        }
    }
}