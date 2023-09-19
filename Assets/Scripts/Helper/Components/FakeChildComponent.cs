using Unity.Entities;

public partial struct FakeChildComponent : IComponentData
{
    public Entity parent;
}