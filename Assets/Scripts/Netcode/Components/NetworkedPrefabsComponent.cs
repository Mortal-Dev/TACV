using Unity.Entities;
using Unity.Collections;

public partial struct NetworkedPrefabsComponent : IComponentData
{
    public NativeHashMap<int, Entity> NetworkedPrefabs;
}