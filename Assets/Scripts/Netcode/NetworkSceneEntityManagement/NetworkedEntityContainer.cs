using System.Collections;
using System.Collections.Generic;
using Unity.Transforms;
using Unity.Collections;
using Unity.Entities;
using Riptide;

public abstract class NetworkedEntityContainer
{
    public readonly Dictionary<ulong, Entity> NetworkedEntities;

    public readonly Dictionary<ulong, bool> SceneEntitiesActive;

    public NetworkedEntityContainer()
    {
        NetworkedEntities = new Dictionary<ulong, Entity>();

        SceneEntitiesActive = new Dictionary<ulong, bool>();
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

    public abstract ulong ActivateNetworkedEntity(Entity entity);

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

    public void SetupSceneNetworkedEntities()
    {
        SceneEntitiesActive.Clear();

        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        EntityQuery entityQuery = entityManager.CreateEntityQuery(typeof(NetworkedEntityComponent));

        NativeArray<Entity> networkedEntities = entityQuery.ToEntityArray(Allocator.Temp);

        foreach (Entity entity in networkedEntities)
        {
            NetworkedEntityComponent networkedEntityComponent = entityManager.GetComponentData<NetworkedEntityComponent>(entity);
            SceneEntitiesActive.Add(networkedEntityComponent.networkEntityId, true);
            ActivateNetworkedEntity(entity);
        }

        networkedEntities.Dispose();
    }
}