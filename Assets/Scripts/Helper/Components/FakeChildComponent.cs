using Unity.Entities;

public partial struct FakeChildComponent : IComponentData
{
    public bool initialized;

    public Entity parent;
}