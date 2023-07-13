using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public partial struct NetworkedEntityComponent : IComponentData
{
    public ushort connectionId;

    public ulong networkEntityId;

    public int networkedPrefabHash;
}
