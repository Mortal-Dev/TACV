using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using Unity.Entities;
using Riptide;

public abstract class NetworkedEntityContainer
{
    private readonly Dictionary<ulong, Entity> NetworkedEntities;

    public NetworkedEntityContainer()
    {
        NetworkedEntities = new Dictionary<ulong, Entity>();
    }

    public IEnumerator<KeyValuePair<ulong, Entity>> GetEntities()
    {
        return NetworkedEntities.GetEnumerator();
    }

    public Entity GetEntity(ulong id)
    {
        return NetworkedEntities[id];
    }

    public abstract ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue);

    public abstract ulong ActiveNetworkedEntity(Entity entity);

    public ulong CreateNetworkedEntityFromIndex(short prefabIndex, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        using EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkedPrefabsComponent>());

        NetworkedPrefabsComponent networkedPrefabsComponent = entityQuery.GetSingleton<NetworkedPrefabsComponent>();

        int prefabHash = networkedPrefabsComponent.hashCodes[prefabIndex];

        return CreateNetworkedEntity(prefabHash, connectionOwnerId, networkEntityId);
    }

    public ulong CreateNetworkedEntity(Entity networkedEntityPrefab, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        using EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkedPrefabsComponent>());

        NetworkedPrefabsComponent networkedPrefabsComponent = entityQuery.GetSingleton<NetworkedPrefabsComponent>();

        for (int i = 0; i < networkedPrefabsComponent.prefabs.Length; i++)
        {
            if (networkedPrefabsComponent.prefabs[i].Equals(networkedEntityPrefab)) return CreateNetworkedEntity(networkedPrefabsComponent.hashCodes[i], connectionOwnerId, networkEntityId);
        }

        throw new System.Exception("unable to find entity from prefab");
    }

    public abstract void DestroyNetworkedEntity(ulong id);

    public abstract void DestroyAllNetworkedEntities();
}