using UnityEngine;
using Unity.Entities;
using System;

public class NetworkedEntityAuthoring : MonoBehaviour
{
    public GameObject OriginalNetworkedPrefab;

    public NetworkedPrefabsAuthoring NetworkedPrefabs;

    class Baking : Baker<NetworkedEntityAuthoring>
    {
        public override void Bake(NetworkedEntityAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            if (authoring.OriginalNetworkedPrefab == null)
                throw new Exception($"{nameof(authoring.OriginalNetworkedPrefab)} has not been set");

            if (authoring.NetworkedPrefabs == null)
                throw new Exception($"{nameof(authoring.NetworkedPrefabs)} has not been set");

            if (!TryFindGameObjectInNetworkedPrefabs(authoring.OriginalNetworkedPrefab, authoring.NetworkedPrefabs))
                throw new Exception($"the prefab {authoring.OriginalNetworkedPrefab.name} could not be found in Networked Prefabs");

            AddComponent(entity, new NetworkedEntityComponent() { connectionId = NetworkManager.SERVER_NET_ID, networkedPrefabHash = authoring.OriginalNetworkedPrefab.name.GetHashCode() });            
        }

        private bool TryFindGameObjectInNetworkedPrefabs(GameObject networkedGameObject, NetworkedPrefabsAuthoring networkedPrefabsAuthoring)
        {
            int networkedGameObjectHashCode = networkedGameObject.GetHashCode();

            foreach (GameObject prefabNetworkedGameObject in networkedPrefabsAuthoring.NetworkedPrefabs)
            {
                if (prefabNetworkedGameObject.name.GetHashCode() == networkedGameObjectHashCode) return true;
            }

            return false;
        }
    }
}