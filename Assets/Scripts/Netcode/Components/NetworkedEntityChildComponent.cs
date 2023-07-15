using Unity.Entities;
using Unity.Collections;

public partial struct NetworkedEntityChildComponent : IComponentData
{
    public FixedList128Bytes<short> childEntityMap;

    public int Id;
}