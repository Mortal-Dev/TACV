using System.Collections;
using System.Collections.Generic;
using Unity.Entities;

public abstract class NetworkedObjectContainer
{
    private readonly Dictionary<ulong, Entity> NetworkedEntities;

    public NetworkedObjectContainer()
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
}