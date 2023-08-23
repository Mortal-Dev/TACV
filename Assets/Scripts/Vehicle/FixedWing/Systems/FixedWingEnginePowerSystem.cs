using System.Runtime.InteropServices;
using Unity.Burst;
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

    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<FixedWingComponent>(), ComponentType.ReadWrite<PhysicsMass>(), ComponentType.ReadWrite<PhysicsVelocity>(), 
            ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new FixedWingEnginePowerJob() { entityManager = systemState.EntityManager, deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new FixedWingEnginePowerJob() { entityManager = systemState.EntityManager, deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    [StructLayout(LayoutKind.Auto)]
    [BurstCompile]
    partial struct FixedWingEnginePowerJob : IJobEntity
    {
        public EntityManager entityManager;

        public float deltaTime;

        public void Execute(ref FixedWingComponent fixedWingComponent, ref PhysicsMass physicsMass, ref PhysicsVelocity physicsVelocity, in LocalTransform localTransform)
        {
            foreach (Entity engineEntity in fixedWingComponent.engineEntities)
            {
                EngineComponent engineComponent = entityManager.GetComponentData<EngineComponent>(engineEntity);

                LocalTransform engineLocalTransform = entityManager.GetComponentData<LocalTransform>(engineEntity);

                engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.throttle;

                physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, ((Vector3)engineLocalTransform.Forward()).normalized * (engineComponent.maxAfterBurnerPowerNewtons * engineComponent.currentPower) * deltaTime, engineLocalTransform.Position);

                break;
            }
        }
    }
}