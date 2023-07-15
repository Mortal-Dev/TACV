using Riptide;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine.SceneManagement;
using UnityEngine;

public class ServerNetworkedEntityContainer : NetworkedEntityContainer
{
    private readonly Dictionary<ulong, Entity> networkedEntities;

    private IdGenerator networkIdGenerator;

    private RefRO<NetworkedPrefabsComponent> networkedPrefabsComponent;

    private EntityManager serverEntityManager;

    public ServerNetworkedEntityContainer(EntityManager serverEntityManager)
    {
        networkedEntities = new Dictionary<ulong, Entity>();

        networkIdGenerator = new IdGenerator();

        this.serverEntityManager = serverEntityManager;
    }

    public Entity GetNetworkedEntity(ulong networkId)
    {
        if (!networkedEntities.TryGetValue(networkId, out Entity value)) throw new Exception($"attempted to find a non-existent entity with the network id: {networkId}");

        return value;
    }

    public override ulong ActivateNetworkedEntity(Entity entity)
    {
        if (!networkedPrefabsComponent.IsValid) SetNetworkedPrefabsComponent();

        NetworkedEntityComponent networkedEntityComponent = serverEntityManager.GetComponentData<NetworkedEntityComponent>(entity);

        if (!networkedPrefabsComponent.ValueRO.TryGetEntity(networkedEntityComponent.networkedPrefabHash, out Entity _)) throw new Exception($"unable to find entity hash for unactivated entity");

        ulong id = networkIdGenerator.GenerateId();

        networkedEntityComponent.networkEntityId = id;

        networkedEntities.Add(id, entity);

        SendSpawnNetworkedEntityMessage(networkedEntityComponent.networkedPrefabHash, networkedEntityComponent.connectionId, serverEntityManager.GetComponentData<LocalTransform>(entity));

        serverEntityManager.SetComponentData(entity, networkedEntityComponent);

        return id;
    }

    public override ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        if (!networkedPrefabsComponent.IsValid) SetNetworkedPrefabsComponent();

        if (!networkedPrefabsComponent.ValueRO.TryGetEntity(networkedPrefabHash, out Entity networkedPrefabEntity)) throw new Exception($"unable to find entity hash {networkedPrefabHash}");
        
        if (!serverEntityManager.HasComponent(networkedPrefabEntity, typeof(NetworkedEntityComponent))) throw new Exception("attempting to instantiate networked entity without a networked entity component");

        Entity entity = serverEntityManager.Instantiate(networkedPrefabEntity);

        ulong id = networkIdGenerator.GenerateId();

        serverEntityManager.SetComponentData(entity, new NetworkedEntityComponent() { connectionId = connectionOwnerId, networkEntityId = id });

        networkedEntities.Add(id, entity);

        SendSpawnNetworkedEntityMessage(networkedPrefabHash, connectionOwnerId, serverEntityManager.GetComponentData<LocalTransform>(entity));

        return id;
    }

    public override void DestroyNetworkedEntity(ulong id)
    {
        if (!networkIdGenerator.IsIdInUse(id)) throw new Exception($"the networked id {id} was not found when attempting to destroy a networked entity");

        serverEntityManager.DestroyEntity(networkedEntities[id]);

        networkIdGenerator.DisposeId(id);

        networkedEntities.Remove(id);

        if (SceneEntitiesActive.ContainsKey(id)) SceneEntitiesActive[id] = false;

        SendDestroyNetworkedEntityMessage(id);
    }

    public override void DestroyAllNetworkedEntities()
    {
        foreach (KeyValuePair<ulong, Entity> idEntityPair in networkedEntities)
        {
            DestroyNetworkedEntity(idEntityPair.Key);
        }

        networkIdGenerator = new IdGenerator();
        networkedEntities.Clear();
    }

    private void SetNetworkedPrefabsComponent()
    {
        using EntityQuery entityQuery = serverEntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkedPrefabsComponent>());

        networkedPrefabsComponent = (RefRO<NetworkedPrefabsComponent>)entityQuery.GetSingletonRW<NetworkedPrefabsComponent>();
    }

    private void SendSpawnNetworkedEntityMessage(int prefabHash, ushort connectionOwnerId, LocalTransform localTransform, ushort sendToClientId = NetworkManager.SERVER_NET_ID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerSpawnEntity);
            
        message.Add(prefabHash);
        message.Add(connectionOwnerId);
        message.AddLocalTransform(localTransform);

        NetworkManager.Instance.Network.SendMessage(message, SendMode.Server, sendToClientId);
    }

    private void SendDestroyNetworkedEntityMessage(ulong id)
    {
        Message message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerDestroyEntity);

        message.AddULong(id);

        NetworkManager.Instance.Network.SendMessage(message, SendMode.Server);
    }

    [MessageHandler((ushort)NetworkMessageId.ClientSyncOwnedEntities)]
    private static void ServerRecieveSyncEntities(ushort clientId, Message message)
    {
        ulong networkedEntityId = message.GetULong();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId);

        NetworkedEntityComponent networkedEntityComponent = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(networkedEntity);

        if (networkedEntityComponent.connectionId != clientId)
        {
            Debug.LogWarning($"client: {clientId} attemtped to set networked entity it did not own");
            return;
        }

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

    private static Entity GetChildFromChildMap(Entity parentRoot, short[] entityChildMap)
    {
        foreach (short siblingIndex in entityChildMap)
        {
            DynamicBuffer<Child> childBuffer = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetBuffer<Child>(parentRoot);

            parentRoot = childBuffer[siblingIndex].Value;
        }

        return parentRoot;
    }
}