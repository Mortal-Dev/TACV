using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
[BurstCompile]
public partial struct FixedWingLiftSystem : ISystem
{
    EntityQuery applyLiftNetworkedEntityQuery;

    ComponentLookup<LocalTransform> localTransfromComponentLookup;

    ComponentLookup<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        applyLiftNetworkedEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<PhysicsVelocity>(), ComponentType.ReadOnly<PhysicsMass>(), ComponentType.ReadOnly<FixedWingComponent>(),
            ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        localTransfromComponentLookup = systemState.GetComponentLookup<LocalTransform>();

        liftGeneratingSurfaceComponentLookup = systemState.GetComponentLookup<LiftGeneratingSurfaceComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        localTransfromComponentLookup.Update(ref systemState);
        liftGeneratingSurfaceComponentLookup.Update(ref systemState);

        new ApplyLiftJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            liftGeneratingSurfaceComponentLookup = liftGeneratingSurfaceComponentLookup,
            localTransformComponentLookup = localTransfromComponentLookup
        }.ScheduleParallel(systemState.Dependency).Complete();
    }
}

[StructLayout(LayoutKind.Auto)]
[BurstCompile]
partial struct ApplyLiftJob : IJobEntity
{
    [NativeDisableContainerSafetyRestriction]
    [ReadOnly]
    public ComponentLookup<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponentLookup;

    [NativeDisableContainerSafetyRestriction]
    [ReadOnly]
    public ComponentLookup<LocalTransform> localTransformComponentLookup;

    [ReadOnly] public float deltaTime;

    public void Execute(ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in FixedWingLiftComponent fixedWingLiftComponent, in FixedWingComponent fixedWingComponent,
        in LocalTransform localTransform, in LocalOwnedNetworkedEntityComponent localOwnedNetworkedEntityComponent)
    {
        foreach (Entity liftGeneratingEntity in fixedWingComponent.liftGeneratingSurfaceEntities)
        {
            RefRW<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponent = liftGeneratingSurfaceComponentLookup.GetRefRW(liftGeneratingEntity);

            LocalTransform liftGeneratingSurfaceLocalTransform = localTransformComponentLookup[liftGeneratingEntity];

            LocalTransform liftGeneratingSurfaceGlobalTransform = liftGeneratingSurfaceLocalTransform.TransformTransform(localTransform);

            if (liftGeneratingSurfaceComponent.ValueRO.lastGlobalTransform.Position.Equals(float3.zero)) liftGeneratingSurfaceComponent.ValueRW.lastGlobalTransform = liftGeneratingSurfaceGlobalTransform;

            float differenceMeters = Vector3.Distance(liftGeneratingSurfaceGlobalTransform.Position, liftGeneratingSurfaceComponent.ValueRO.lastGlobalTransform.Position);

            float metersPerSecond = differenceMeters / deltaTime;

            float liftCoefficient = fixedWingLiftComponent.pitchLiftCurve.Evaluate(fixedWingComponent.angleOfAttack) * liftGeneratingSurfaceComponent.ValueRO.PitchAoALiftCoefficientPercentageCurve.Evaluate(fixedWingComponent.angleOfAttack);

            float liftForce = 0.5f * AirDensity.GetAirDensityFromMeters(liftGeneratingSurfaceGlobalTransform.Position.y) * (metersPerSecond * metersPerSecond) * liftGeneratingSurfaceComponent.ValueRO.liftArea * liftCoefficient;

            physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, localTransform.TransformDirection(Quaternion.Euler(new float3(-90, 0, 0)) * fixedWingComponent.localVelocity).Normalize() * liftForce * deltaTime, liftGeneratingSurfaceLocalTransform.Position);

            liftGeneratingSurfaceComponent.ValueRW.lastGlobalTransform = liftGeneratingSurfaceGlobalTransform;
        }
    }
}