using UnityEngine;
using Unity.Entities;
using System;

public class NetworkedEntityAuthoring : MonoBehaviour
{
    public GameObject OriginalNetworkedPrefab;

    class Baking : Baker<NetworkedEntityAuthoring>
    {
        public override void Bake(NetworkedEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.OriginalNetworkedPrefab == null)
                throw new Exception($"{nameof(authoring.OriginalNetworkedPrefab)} has not been set");

            AddComponent(entity, new NetworkedEntityComponent() { connectionId = NetworkManager.SERVER_NET_ID, networkedPrefabHash = authoring.OriginalNetworkedPrefab.name.GetHashCode() });
        }
    }
}