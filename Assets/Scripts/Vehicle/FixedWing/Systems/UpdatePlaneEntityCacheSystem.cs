using Unity.Entities;
using Unity.Burst;
using Unity.Collections;

public partial struct UpdatePlaneEntityCacheSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (uninitializedfixedWingComponent, fixedWingComponent, entity) in SystemAPI.Query<RefRO<UninitializedFixedWingComponent>, RefRW<FixedWingComponent>>().WithEntityAccess())
        {
            entityCommandBuffer.RemoveComponent<UninitializedFixedWingComponent>(entity);


        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }
}