using System.Runtime.InteropServices;
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
public partial struct FixedWingEnginePowerSystem : ISystem
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

        FixedWingEnginePowerJob fixedWingEnginePowerJob = new FixedWingEnginePowerJob() { deltaTime = SystemAPI.Time.DeltaTime, engineComponentLookup = engineComponentLookup, localTransformLookup = localTransformLookup };

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            fixedWingEnginePowerJob.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            fixedWingEnginePowerJob.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [BurstCompile]
    partial struct FixedWingEnginePowerJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        [ReadOnly] public ComponentLookup<EngineComponent> engineComponentLookup;

        [ReadOnly] public ComponentLookup<LocalTransform> localTransformLookup;

        public void Execute(ref FixedWingComponent fixedWingComponent, ref PhysicsMass physicsMass, ref PhysicsVelocity physicsVelocity, in LocalTransform localTransform)
        {
            foreach (Entity engineEntity in fixedWingComponent.engineEntities)
            {
                if (!engineComponentLookup.TryGetComponent(engineEntity, out EngineComponent engineComponent))
                {
                    Debug.LogError("engine entity does not have engine component");
                    return;
                }

                if (!localTransformLookup.TryGetComponent(engineEntity, out LocalTransform engineLocalTransform))
                {
                    Debug.LogError("uanble to find transform for engine entity");
                    return;
                }

                engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.throttle;

                physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, ((Vector3)engineLocalTransform.Forward()).normalized * (engineComponent.maxAfterBurnerPowerNewtons * engineComponent.currentPower) * deltaTime, engineLocalTransform.Position);

                break;
            }
        }
    }
}