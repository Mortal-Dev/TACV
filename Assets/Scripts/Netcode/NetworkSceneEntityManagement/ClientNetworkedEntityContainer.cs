using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Riptide;
using UnityEngine.SceneManagement;

public class ClientNetworkedEntityContainer : NetworkedEntityContainer
{
    private readonly Dictionary<ulong, Entity> networkedEntities;

    private RefRO<NetworkedPrefabsComponent> networkedPrefabsComponent;

    private EntityManager clientEntityManager;

    public ClientNetworkedEntityContainer(EntityManager clientEntityManager)
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

        if (!networkedPrefabsComponent.ValueRO.TryGetEntity(networkedPrefabHash, out Entity networkedEntityPrefab)) throw new Exception($"unable to find prefab hash: {networkedPrefabHash}");

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
        UnityEngine.Debug.Log("server spawn");

        int networkedPrefabHash = message.GetInt();
        ushort ownerId = message.GetUShort();
        LocalTransform localTransform = message.GetLocalTransform();

        ulong networkEntityId = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.CreateNetworkedEntity(networkedPrefabHash, ownerId);
        Entity networkEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkEntityId);

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkEntity, localTransform);
    }

    [MessageHandler((ushort)NetworkMessageId.ServerDestroyEntity)]
    private static void DestroyNetworkedEntityRecieved(Message message)
    {
        ulong networkedEntityId = message.GetULong();

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.DestroyEntity(NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId));
    }

    [MessageHandler((ushort)NetworkMessageId.ServerSyncEntity)]
    private static void ClientRecieveSyncEntities(Message message)
    {
        ulong networkedEntityId = message.GetULong();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId);

        int length = message.GetInt();

        for (int i = 0; i < length; i++)
        {
            LocalTransform localTransform = message.GetLocalTransform();
            short[] entityChildMap = message.GetShorts();

            if (entityChildMap[0] == -1)
            {
                //-1 is the parent, so we just set that
                NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkedEntity, localTransform);
                continue;
            }

            NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(GetChildFromChildMap(networkedEntity, entityChildMap), localTransform);
        }
    }

    [MessageHandler((ushort)NetworkMessageId.ServerDestroyDefaultSceneEntity)]
    private static void ClientRecieveServerDestroyDefaultSceneEntity(Message message)
    {
        ulong networkId = message.GetULong();

        NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.DestroyNetworkedEntity(networkId);
    }

    private static Entity GetChildFromChildMap(Entity parentRoot, short[] entityChildMap)
    {
        foreach (short siblingIndex in entityChildMap)
        {
            DynamicBuffer<Child> childBuffer = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetBuffer<Child>(parentRoot);

            parentRoot = childBuffer[siblingIndex].Value;
        }

        return parentRoot;
    }

    public override ulong ActivateNetworkedEntity(Entity entity)
    {
        throw new Exception("cannot active entites from client, must be server");
    }
}