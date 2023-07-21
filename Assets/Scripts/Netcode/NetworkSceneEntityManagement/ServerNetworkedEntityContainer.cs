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
        NetworkedEntityComponent networkedEntityComponent = serverEntityManager.GetComponentData<NetworkedEntityComponent>(entity);

        if (GetNetworkedPrefab(networkedEntityComponent.networkedPrefabHash) == Entity.Null) throw new Exception($"unable to find entity hash for unactivated entity with index: " + entity.Index);

        ulong id = networkIdGenerator.GenerateId();

        networkedEntityComponent.networkEntityId = id;

        networkedEntities.Add(id, entity);

        SendSpawnNetworkedEntityMessage(networkedEntityComponent.networkedPrefabHash, networkedEntityComponent.connectionId, serverEntityManager.GetComponentData<LocalTransform>(entity));

        serverEntityManager.SetComponentData(entity, networkedEntityComponent);

        return id;
    }

    public override ulong CreateNetworkedEntity(int networkedPrefabHash, ushort connectionOwnerId = NetworkManager.SERVER_NET_ID, ulong networkEntityId = ulong.MaxValue)
    {
        if (TryGetNetworkedPrefab(networkedPrefabHash, out Entity networkedPrefabEntity)) throw new Exception($"unable to find entity hash {networkedPrefabHash}");

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
        if (NetworkManager.CLIENT_NET_ID == clientId) return;

        NetworkSceneManager networkSceneManager = NetworkManager.Instance.NetworkSceneManager;

        ulong networkedEntityId = message.GetULong();

        Entity networkedEntity = networkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId);

        bool updateParentEntity = message.GetBool();

        if (updateParentEntity) networkSceneManager.NetworkWorld.EntityManager.SetComponentData(networkedEntity, message.GetLocalTransform());

        int length = message.GetInt();

        for (int i = 0; i < length; i++)
        {
            Entity child = GetChildFromChildMap(networkedEntity, message.GetInts());
            networkSceneManager.NetworkWorld.EntityManager.SetComponentData(child, message.GetLocalTransform());
        }
    }

    private static Entity GetChildFromChildMap(Entity parent, int[] map)
    {
        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        DynamicBuffer<Child> children = entityManager.GetBuffer<Child>(parent);

        DynamicBuffer<Child> newChildren = default;

        foreach (int childId in map)
        {
            if (newChildren.Length != 0) children = newChildren;

            foreach (Child child in children)
            {
                if (!entityManager.HasComponent<NetworkedEntityChildComponent>(child.Value)) continue;

                NetworkedEntityChildComponent networkedEntityChildComponent = entityManager.GetComponentData<NetworkedEntityChildComponent>(child.Value);

                if (networkedEntityChildComponent.Id != childId) continue;

                if (childId == map[^1]) return child.Value;

                newChildren = entityManager.GetBuffer<Child>(child.Value);

                break;
            }
        }

        throw new Exception("unable to find child entity");
    }
}