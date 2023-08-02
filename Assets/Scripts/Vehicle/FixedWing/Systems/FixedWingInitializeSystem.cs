using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using System;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PlanePhysicsSystem))]
public partial class FixedWingInitializeSystem : SystemBase
{
    protected override void OnUpdate()
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        Entities.ForEach((Entity entity, ref FixedWingComponent fixedWingComponent, in UninitializedFixedWingComponent uninitializedfixedWingComponent) =>
        {
            entityCommandBuffer.RemoveComponent<UninitializedFixedWingComponent>(entity);

            DynamicBuffer<LinkedEntityGroup> children = EntityManager.GetBuffer<LinkedEntityGroup>(entity);

            SetFixedWingEntities(ref fixedWingComponent, children);


        }).WithoutBurst().Run();

        entityCommandBuffer.Playback(EntityManager);
        entityCommandBuffer.Dispose();
    }
    
    //looks slow, but takes less than 2 milliseconds, and only runs once when the aircraft is initialized, but I could probably make a more performant version later
    private void SetFixedWingEntities(ref FixedWingComponent fixedWingComponent, DynamicBuffer<LinkedEntityGroup> children)
    {
        fixedWingComponent.flapEntities = GetEntities<FlapComponent>(children);
        fixedWingComponent.engineEntities = GetEntities<EngineComponent>(children);
        fixedWingComponent.rudderEntities = GetEntities<RudderComponent>(children);
        fixedWingComponent.stabilatorEntities = GetEntities<StabilatorComponent>(children);
        fixedWingComponent.airleronEntities = GetEntities<AirleronComponent>(children);
        fixedWingComponent.engineEntities = GetEntities<EngineComponent>(children);
    }

    private FixedList128Bytes<Entity> GetEntities<T>(DynamicBuffer<LinkedEntityGroup> children) where T : struct, IComponentData
    {
        FixedList128Bytes<Entity> entities = new FixedList128Bytes<Entity>();

        foreach (LinkedEntityGroup linkedEntityGroup in children)
        {
            if (!EntityManager.HasComponent<T>(linkedEntityGroup.Value)) continue;

            entities.Add(linkedEntityGroup.Value);
        }

        return entities;
    }
}