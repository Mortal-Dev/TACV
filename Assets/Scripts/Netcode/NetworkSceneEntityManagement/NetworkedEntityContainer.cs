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

    public abstract void DestroyNetworkedEntity(ulong id);

    public abstract void DestroyAllNetworkedEntities();

    [MessageHandler((ushort)NetworkMessageId.SyncEntities)]
    private static void ServerRecieveSyncEntities(ushort fromClient, Message message)
    {
        ulong networkedEntityId = message.GetULong();

        Entity networkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityId);

        if (NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(networkedEntity).connectionId != fromClient) return;

        RecieveSyncEntities(message);
    }

    [MessageHandler((ushort)NetworkMessageId.SyncEntities)]
    private static void RecieveSyncEntities(Message message)
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