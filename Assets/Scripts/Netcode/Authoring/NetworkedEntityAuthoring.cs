﻿using UnityEngine;
using Unity.Entities;
using System;
using Unity.Transforms;

public class NetworkedEntityAuthoring : MonoBehaviour
{
    public GameObject OriginalNetworkedPrefab;

    class Baking : Baker<NetworkedEntityAuthoring>
    {
        static System.Random random = new System.Random();

        public override void Bake(NetworkedEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.OriginalNetworkedPrefab == null)
                throw new Exception($"{nameof(authoring.OriginalNetworkedPrefab)} has not been set for {authoring.gameObject.name}");

            Debug.Log($"prefab hash for {authoring.OriginalNetworkedPrefab.name} is {authoring.OriginalNetworkedPrefab.name.GetHashCode()}");

            AddComponent(entity, new NetworkedEntityComponent() { connectionId = NetworkManager.SERVER_NET_ID, networkedPrefabHash = authoring.OriginalNetworkedPrefab.name.GetHashCode(), networkEntityId = (ulong)random.Next(0, int.MaxValue) });
            AddComponent(entity, new PreviousLocalTransformRecordComponent() { localTransformRecord = new LocalTransform() { Position = authoring.transform.localPosition, Rotation = authoring.transform.localRotation, Scale = authoring.transform.localScale.y } });
        }
    }
}