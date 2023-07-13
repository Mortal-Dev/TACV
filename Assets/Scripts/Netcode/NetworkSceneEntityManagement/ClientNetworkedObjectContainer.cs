using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Riptide;
using UnityEngine.SceneManagement;

public class ClientNetworkedObjectContainer : NetworkedObjectContainer
{
    private readonly Dictionary<ulong, Entity> networkedEntities;

    private RefRO<NetworkedPrefabsComponent> networkedPrefabsComponent;

    private EntityManager clientEntityManager;

    public ClientNetworkedObjectContainer(EntityManager clientEntityManager)
    {
        networkedEntities = new Dictionary<ulong, Entity>();

        this.clientEntityManager = clientEntityManager;
    }

    public Entity GetNetworkedEntity(uint networkId)
    {
        if (!networkedEntities.TryGetValue(networkId, out Entity value)) throw new Exception($"attempted to find a non-existent entity with the network id: {networkId}");

        return value;
    }

    public override ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        if (!networkedPrefabsComponent.IsValid) SetNetworkedPrefabsComponent();

        if (!networkedPrefabsComponent.ValueRO.NetworkedPrefabs.TryGetValue(networkedPrefabHash, out Entity networkedEntityPrefab)) throw new Exception($"unable to find prefab hash: {networkedPrefabHash}");

        Entity spawnedNetworkedEntity = clientEntityManager.Instantiate(networkedEntityPrefab);

        clientEntityManager.SetComponentData(spawnedNetworkedEntity, new NetworkedEntityComponent() { networkEntityId = networkEntityId, connectionId = connectionOwnerId });

        if (networkEntityId == NetworkManager.CLIENT_NET_ID) clientEntityManager.AddComponent(spawnedNetworkedEntity, ComponentType.ReadWrite(typeof(LocalOwnedNetworkedEntityComponent)));
        
        networkedEntities.Add(networkEntityId, spawnedNetworkedEntity);

        return networkEntityId;
    }

    public override void DestroyNetworkedEntity(ulong networkId)
    {
        if (!networkedEntities.TryGetValue(networkId, out Entity networkedEntity)) throw new Exception($"attempted to destroy an entity with the id {networkId} that doesn't exist");

        clientEntityManager.DestroyEntity(networkedEntity);

        networkedEntities.Remove(networkId);
    }

    public override void DestroyAllNetworkedEntities()
    {
        foreach (KeyValuePair<ulong, Entity> idEntityPair in networkedEntities)
        {
            DestroyNetworkedEntity(idEntityPair.Key);
        }

        networkedEntities.Clear();
    }

    private void SetNetworkedPrefabsComponent()
    {
        using EntityQuery entityQuery = clientEntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkedPrefabsComponent>());

        networkedPrefabsComponent = (RefRO<NetworkedPrefabsComponent>)entityQuery.GetSingletonRW<NetworkedPrefabsComponent>();
    }

    [MessageHandler((ushort)NetworkMessageId.ServerSpawnEntity)]
    private static void SpawnNetworkedEntityRecieved(Message message)
    {
        int entityHash = message.GetInt();
        ushort ownerId = message.GetUShort();
        LocalTransform localTransform = message.GetLocalTransform();

        ulong networkEntityId = NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.CreateNetworkedEntity(entityHash, ownerId);
        Entity networkEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.GetEntity(networkEntityId);

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkEntity, localTransform);
    }

    [MessageHandler((ushort)NetworkMessageId.ServerDestroyEntity)]
    private static void DestroyNetworkedEntityRecieved(Message message)
    {
        ulong networkedEntityId = message.GetULong();

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.DestroyEntity(NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.GetEntity(networkedEntityId));
    }

    [MessageHandler((ushort)NetworkMessageId.SyncEntities)]
    private static void ClientRecieveSyncEntities(Message message)
    {
        ulong networkedEntityId = message.GetULong();
        LocalTransform localTransform = message.GetLocalTransform();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.GetEntity(networkedEntityId);

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkedEntity, localTransform);
    }
}