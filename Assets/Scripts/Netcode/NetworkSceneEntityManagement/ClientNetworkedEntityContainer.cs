using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Riptide;
using UnityEngine.SceneManagement;

public class ClientNetworkedEntityContainer : NetworkedEntityContainer
{
    private readonly Dictionary<ulong, Entity> networkedEntities;

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
        Entity entityPrefab = GetNetworkedPrefab(networkedPrefabHash);

        if (entityPrefab == Entity.Null) throw new Exception($"unable to find entity hash for prefab entity with hash: " + networkedPrefabHash);

        Entity spawnedNetworkedEntity = clientEntityManager.Instantiate(entityPrefab);

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
        if (NetworkManager.Instance.NetworkType == NetworkType.Host) return;

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

    [MessageHandler((ushort)NetworkMessageId.ServerDestroyDefaultSceneEntity)]
    private static void ClientRecieveServerDestroyDefaultSceneEntity(Message message)
    {
        ulong networkId = message.GetULong();

        NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.DestroyNetworkedEntity(networkId);
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

    public override ulong ActivateNetworkedEntity(Entity entity)
    {
        throw new Exception("cannot active entites from client, must be server");
    }
}