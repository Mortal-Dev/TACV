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

    public override ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        if (!networkedPrefabsComponent.IsValid) SetNetworkedPrefabsComponent();

        if (!networkedPrefabsComponent.ValueRO.NetworkedPrefabs.TryGetValue(networkedPrefabHash, out Entity networkedPrefabEntity)) throw new Exception($"unable to find entity hash {networkedPrefabHash}");
        
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

    [MessageHandler((ushort)NetworkMessageId.SyncEntities)]
    private static void ServerRecieveEntitySyncMessage(ushort fromClientId, Message message)
    {
        ulong networkedEntityId = message.GetULong();
        LocalTransform networkLocalTransform = message.GetLocalTransform();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId);

        NetworkedEntityComponent networkedEntityComponent = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(networkedEntity);

        //don't allow clients to set objects they don't own
        if (networkedEntityComponent.connectionId != fromClientId) return;

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkedEntity, networkLocalTransform);
    }
}