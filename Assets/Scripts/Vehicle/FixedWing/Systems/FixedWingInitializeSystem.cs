﻿using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Diagnostics;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FixedWingStateSystem))]
[UpdateAfter(typeof(VehicleSeatInitializeSystem))]
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

            SetLiftEntities(children);
        }).WithoutBurst().Run();

        entityCommandBuffer.Playback(EntityManager);
        entityCommandBuffer.Dispose();
    }
    
    //looks slow, but takes less than a millisecond, and only runs once when the aircraft is initialized, but I could probably make a more performant version later
    private void SetFixedWingEntities(ref FixedWingComponent fixedWingComponent, DynamicBuffer<LinkedEntityGroup> children)
    {
        fixedWingComponent.flapEntities = GetEntities<FlapComponent>(children);
        fixedWingComponent.engineEntities = GetEntities<EngineComponent>(children);
        fixedWingComponent.rudderEntities = GetEntities<RudderComponent>(children);
        fixedWingComponent.stabilatorEntities = GetEntities<StabilatorComponent>(children);
        fixedWingComponent.airleronEntities = GetEntities<AirleronComponent>(children);
        fixedWingComponent.engineEntities = GetEntities<EngineComponent>(children);
        fixedWingComponent.liftGeneratingSurfaceEntities = GetEntities<LiftGeneratingSurfaceComponent>(children);

        fixedWingComponent.centerOfPressureEntity = GetEntity<CenterOfPressureComponent>(children);
        fixedWingComponent.centerOfGravityEntity = GetEntity<CenterOfGravityComponent>(children);
    }

    //could use dictionary stuff here, but not worth it rn
    public void SetLiftEntities(DynamicBuffer<LinkedEntityGroup> children)
    {
        foreach (LinkedEntityGroup linkedEntityGroup in children)
        {
            if (EntityManager.HasComponent<LiftGeneratingSurfaceComponent>(linkedEntityGroup.Value)) SetLiftEntity(linkedEntityGroup.Value, EntityManager.GetComponentData<LiftGeneratingSurfaceComponent>(linkedEntityGroup.Value), children);
        }
    }

    private void SetLiftEntity(Entity liftGeneratingSurfaceEntity, LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, DynamicBuffer<LinkedEntityGroup> children)
    {
        ComponentId componentId = liftGeneratingSurfaceComponent;

        LiftGeneratingSurfaceComponent newLiftGeneratingSurfaceComponent = liftGeneratingSurfaceComponent;

        foreach (LinkedEntityGroup linkedEntityGroup in children)
        {

            if (EntityManager.HasComponent<MaxCenterOfPressureComponent>(linkedEntityGroup.Value) && (EntityManager.GetComponentData<MaxCenterOfPressureComponent>(linkedEntityGroup.Value) as ComponentId).Id == componentId.Id)
            {
                newLiftGeneratingSurfaceComponent.maxCenterOfLiftEntity = linkedEntityGroup.Value;
            }
            else if (EntityManager.HasComponent<MinCenterOfPressureComponent>(linkedEntityGroup.Value) && (EntityManager.GetComponentData<MinCenterOfPressureComponent>(linkedEntityGroup.Value) as ComponentId).Id == componentId.Id)
            {
                newLiftGeneratingSurfaceComponent.minCenterOfLiftEntity = linkedEntityGroup.Value;
            }

            if (newLiftGeneratingSurfaceComponent.minCenterOfLiftEntity != Entity.Null && newLiftGeneratingSurfaceComponent.maxCenterOfLiftEntity != Entity.Null)
                break;
        }

        EntityManager.SetComponentData(liftGeneratingSurfaceEntity, newLiftGeneratingSurfaceComponent);
    }

    private Entity GetEntity<T>(DynamicBuffer<LinkedEntityGroup> children) where T : unmanaged, IComponentData
    {
        foreach (LinkedEntityGroup linkedEntityGroup in children)
        {
            if (!EntityManager.HasComponent<T>(linkedEntityGroup.Value)) continue;

            return linkedEntityGroup.Value;
        }

        return Entity.Null;
    }

    private FixedList128Bytes<Entity> GetEntities<T>(DynamicBuffer<LinkedEntityGroup> children) where T : unmanaged, IComponentData, ComponentId
    {
        FixedList128Bytes<Entity> entities = new FixedList128Bytes<Entity>();

        foreach (LinkedEntityGroup linkedEntityGroup in children)
        {
            if (!EntityManager.HasComponent<T>(linkedEntityGroup.Value)) continue;

            ComponentId componentId = EntityManager.GetComponentData<T>(linkedEntityGroup.Value);

            entities.Add(linkedEntityGroup.Value);
        }

        entities.OrderBy(entity => EntityManager.GetComponentData<T>(entity).Id);

        return entities;
    }
}