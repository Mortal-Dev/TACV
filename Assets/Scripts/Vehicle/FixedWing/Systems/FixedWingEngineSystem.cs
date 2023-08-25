﻿using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FixedWingStateSystem))]
[BurstCompile]
public partial struct FixedWingEngineSystem : ISystem
{
    EntityQuery networkEntityQuery;

    ComponentLookup<EngineComponent> engineComponentLookup;

    ComponentLookup<LocalTransform> localTransformLookup;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<FixedWingComponent>(), ComponentType.ReadWrite<PhysicsMass>(), ComponentType.ReadWrite<PhysicsVelocity>(), 
            ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        engineComponentLookup = systemState.GetComponentLookup<EngineComponent>();

        localTransformLookup = systemState.GetComponentLookup<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        engineComponentLookup.Update(ref systemState);
        localTransformLookup.Update(ref systemState);

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.TempJob);

        EntityCommandBuffer.ParallelWriter parallelWriterEntityCommandBuffer = entityCommandBuffer.AsParallelWriter();

        FixedWingEngineJob fixedWingEnginePowerJob = new() { deltaTime = SystemAPI.Time.DeltaTime, engineComponentLookup = engineComponentLookup, localTransformLookup = localTransformLookup,
            parallelWriterEntityCommandBuffer = parallelWriterEntityCommandBuffer
        };

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            fixedWingEnginePowerJob.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            fixedWingEnginePowerJob.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    [StructLayout(LayoutKind.Auto)]
    [BurstCompile]
    partial struct FixedWingEngineJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        [ReadOnly] public ComponentLookup<EngineComponent> engineComponentLookup;

        [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;

        public EntityCommandBuffer.ParallelWriter parallelWriterEntityCommandBuffer;

        public void Execute([ChunkIndexInQuery] int sortKey, ref FixedWingComponent fixedWingComponent, ref PhysicsMass physicsMass, ref PhysicsVelocity physicsVelocity, in LocalTransform localTransform)
        {
            foreach (Entity engineEntity in fixedWingComponent.engineEntities)
            {
                EngineComponent engineComponent = engineComponentLookup[engineEntity];

                LocalTransform engineLocalTransform = localTransformLookup[engineEntity];

                engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.throttle;

                parallelWriterEntityCommandBuffer.SetComponent(sortKey, engineEntity, engineComponent);

                physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, ((Vector3)engineLocalTransform.Forward()).normalized * 
                    engineComponent.currentPower * deltaTime, engineLocalTransform.Position);
            }
        }
    }
}