using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FixedWingStateSystem))]
public partial struct FixedWingEnginePowerSystem : ISystem
{
    EntityQuery networkEntityQuery;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<FixedWingComponent>(), ComponentType.ReadWrite<PhysicsMass>(), ComponentType.ReadWrite<PhysicsVelocity>(), 
            ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());
    }

    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new FixedWingEnginePowerJob() { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new FixedWingEnginePowerJob() { deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    partial struct FixedWingEnginePowerJob : IJobEntity
    {
        public float deltaTime;

        public void Execute(ref FixedWingComponent fixedWingComponent, ref PhysicsMass physicsMass, ref PhysicsVelocity physicsVelocity, in LocalTransform localTransform)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            foreach (Entity engineEntity in fixedWingComponent.engineEntities)
            {
                EngineComponent engineComponent = entityManager.GetComponentData<EngineComponent>(engineEntity);

                LocalTransform engineLocalTransform = entityManager.GetComponentData<LocalTransform>(engineEntity);

                engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.throttle;

                physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, ((Vector3)localTransform.Forward()).normalized * engineComponent.maxAfterBurnerPowerNewtons * deltaTime, engineLocalTransform.Position);

                break;
            }
        }
    }
}