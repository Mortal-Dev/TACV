using Riptide;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;

public class HostNetworkedObjectContainer : NetworkedObjectContainer
{
    private readonly Dictionary<ulong, Entity> networkedEntities;

    private IdGenerator networkIdGenerator;

    private RefRO<NetworkedPrefabsComponent> networkedPrefabsComponent;

    private EntityManager hostEntityManager;

    private readonly Server server;

    public HostNetworkedObjectContainer(EntityManager hostEntityManager)
    {
        networkedEntities = new Dictionary<ulong, Entity>();

        networkIdGenerator = new IdGenerator();

        this.hostEntityManager = hostEntityManager;

        server = ((HostNetwork)NetworkManager.Instance.Network).Server;
    }

   public override ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = ushort.MaxValue, ulong networkEntityId = ulong.MaxValue)
    {
        if (!networkedPrefabsComponent.IsValid) SetNetworkedPrefabsComponent();

        if (!networkedPrefabsComponent.ValueRO.NetworkedPrefabs.TryGetValue(networkedPrefabHash, out Entity networkedPrefabEntity)) throw new Exception($"unable to find entity hash {networkedPrefabHash}");

        if (!hostEntityManager.HasComponent(networkedPrefabEntity, typeof(NetworkedEntityComponent))) throw new Exception("attempting to instantiate networked entity without a networked entity component");

        Entity entity = hostEntityManager.Instantiate(networkedPrefabEntity);

        ulong id = networkIdGenerator.GenerateId();

        hostEntityManager.SetComponentData(entity, new NetworkedEntityComponent() { connectionId = connectionOwnerId, networkEntityId = id });

        networkedEntities.Add(id, entity);

        SendSpawnNetworkedEntityMessage(networkedPrefabHash, connectionOwnerId, hostEntityManager.GetComponentData<LocalTransform>(entity));

        return id;
    }

    public override void DestroyNetworkedEntity(ulong id)
    {
        if (!networkIdGenerator.IsIdInUse(id)) throw new Exception($"the networked id {id} was not found when attempting to destroy a networked entity");

        hostEntityManager.DestroyEntity(networkedEntities[id]);

        networkedEntities.Remove(id);

        networkIdGenerator.DisposeId(id);

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
        using EntityQuery entityQuery = hostEntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkedPrefabsComponent>());

        networkedPrefabsComponent = (RefRO<NetworkedPrefabsComponent>)entityQuery.GetSingletonRW<NetworkedPrefabsComponent>();
    }

    private void SendSpawnNetworkedEntityMessage(int prefabHash, ushort connectionOwnerId, LocalTransform localTransform, ushort sendToClientId = NetworkManager.SERVER_NET_ID)
    {
        Message message;

        //don't send spawn to ourself
        if (sendToClientId == NetworkManager.CLIENT_NET_ID) return;

        //send spawn if it's an individual, not everyone
        if (sendToClientId != NetworkManager.SERVER_NET_ID)
        {
            NetworkManager.Instance.Network.SendMessage(CreateSpawnedNetworkedEntityMessage(), SendMode.Server, sendToClientId);
        }
        else
        {  //send spawn to everyone except ourself
            foreach (Connection connection in server.Clients)
            {
                if (connection.Id == NetworkManager.CLIENT_NET_ID) continue;

                NetworkManager.Instance.Network.SendMessage(CreateSpawnedNetworkedEntityMessage(), SendMode.Server, connection.Id);
            }
        }

        Message CreateSpawnedNetworkedEntityMessage()
        {
            message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerSpawnEntity);

            message.Add(prefabHash);
            message.Add(connectionOwnerId);
            message.AddLocalTransform(localTransform);

            return message;
        }

    }

    private void SendDestroyNetworkedEntityMessage(ulong id)
    {
        foreach (Connection connection in server.Clients)
        {
            if (connection.Id == NetworkManager.CLIENT_NET_ID) continue;

            NetworkManager.Instance.Network.SendMessage(Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerDestroyEntity).AddULong(id), SendMode.Server, connection.Id);
        }
    }

    [MessageHandler((ushort)NetworkMessageId.SyncEntities)]
    private static void ServerRecieveEntitySyncMessage(ushort fromClientId, Message message)
    {
        //since we're hosting, don't set position of our own networked entites
        if (fromClientId == NetworkManager.CLIENT_NET_ID || fromClientId == NetworkManager.SERVER_NET_ID) return;

        ulong networkedEntityId = message.GetULong();
        LocalTransform networkLocalTransform = message.GetLocalTransform();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.GetEntity(networkedEntityId);

        NetworkedEntityComponent networkedEntityComponent = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(networkedEntity);

        //don't allow clients to set objects they don't own
        if (networkedEntityComponent.connectionId != fromClientId) return;

        NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkedEntity, networkLocalTransform);
    }
}