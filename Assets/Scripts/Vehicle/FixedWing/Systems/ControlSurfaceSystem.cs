using Unity.Entities;
using Unity.Transforms;

public partial struct ControlSurfaceSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        foreach (var (fixedWingInputComponent, fixedWingComponent, entity) in SystemAPI.Query<RefRO<FixedWingInputComponent>, RefRO<FixedWingComponent>>().WithEntityAccess())
        {

        }
    }
}
