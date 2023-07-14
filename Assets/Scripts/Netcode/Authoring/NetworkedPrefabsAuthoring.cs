using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using UnityEngine;

public class NetworkedPrefabsAuthoring : MonoBehaviour
{
    public GameObject[] NetworkedPrefabs;

    class Baking : Baker<NetworkedPrefabsAuthoring>
    {
        public override void Bake(NetworkedPrefabsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            NetworkedPrefabsComponent networkedPrefabsComponent = new NetworkedPrefabsComponent() { hashCodes = new FixedList512Bytes<int>(), prefabs = new FixedList512Bytes<Entity>() };

            foreach (GameObject gameObject in authoring.NetworkedPrefabs)
            {
                networkedPrefabsComponent.AddPrefab(gameObject.name.GetHashCode(), GetEntity(gameObject, TransformUsageFlags.Dynamic));
            }

            AddComponent(entity, networkedPrefabsComponent);
        }
    }
}