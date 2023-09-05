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
    EntityQuery updateliftNetworkEntityQuery;

    EntityQuery applyLiftNetworkedEntityQuery;

    ComponentLookup<FixedWingLiftComponent> fixedWingLiftComponentLookup;

    ComponentLookup<LocalTransform> localTransfromComponentLookup;

    ComponentLookup<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponentLookup;

    ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        updateliftNetworkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<LiftGeneratingSurfaceComponent>(), ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<Parent>(),
            ComponentType.ReadOnly<NetworkedEntityChildComponent>());

        applyLiftNetworkedEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<PhysicsVelocity>(), ComponentType.ReadOnly<PhysicsMass>(), ComponentType.ReadOnly<FixedWingComponent>(), 
            ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        fixedWingLiftComponentLookup = systemState.GetComponentLookup<FixedWingLiftComponent>();

        localTransfromComponentLookup = systemState.GetComponentLookup<LocalTransform>();

        liftGeneratingSurfaceComponentLookup = systemState.GetComponentLookup<LiftGeneratingSurfaceComponent>();

        fixedWingComponentLookup = systemState.GetComponentLookup<FixedWingComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        fixedWingLiftComponentLookup.Update(ref systemState);
        localTransfromComponentLookup.Update(ref systemState);
        liftGeneratingSurfaceComponentLookup.Update(ref systemState);
        fixedWingComponentLookup.Update(ref systemState);

        UpdateLiftJob updateLiftJob = new() { deltaTime = SystemAPI.Time.DeltaTime, fixedWingLiftComponentLookup = fixedWingLiftComponentLookup, 
            localTransformComponentLookup = localTransfromComponentLookup, fixedWingComponentLookup = fixedWingComponentLookup };
        ApplyLiftJob applyLiftJob = new() { deltaTime = SystemAPI.Time.DeltaTime, liftGeneratingSurfaceComponentLookup = liftGeneratingSurfaceComponentLookup, localTransformComponentLookup = localTransfromComponentLookup };

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            updateLiftJob.ScheduleParallel(systemState.Dependency).Complete();
            applyLiftJob.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            updateLiftJob.ScheduleParallel(updateliftNetworkEntityQuery, systemState.Dependency).Complete();
            applyLiftJob.ScheduleParallel(applyLiftNetworkedEntityQuery, systemState.Dependency).Complete();
        }
    }
}

[StructLayout(LayoutKind.Auto)]
[BurstCompile]
partial struct UpdateLiftJob : IJobEntity
{
    [ReadOnly] public float deltaTime;

    [NativeDisableContainerSafetyRestriction]
    [ReadOnly] 
    public ComponentLookup<LocalTransform> localTransformComponentLookup;
    
    [ReadOnly] public ComponentLookup<FixedWingLiftComponent> fixedWingLiftComponentLookup;

    [ReadOnly] public ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

    public void Execute(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform localTransform, in Parent parent)
    {
        FixedWingLiftComponent fixedWingLiftComponent = fixedWingLiftComponentLookup[parent.Value];

        FixedWingComponent fixedWingComponent = fixedWingComponentLookup[parent.Value];

        ProcessLift(ref liftGeneratingSurfaceComponent, ref localTransform, in parent, fixedWingLiftComponent, fixedWingComponent);
    }

    [BurstCompile]
    private void ProcessLift(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform liftGeneratingSurfaceLocalTransform,
        in Parent parent, FixedWingLiftComponent fixedWingLiftComponent, FixedWingComponent fixedWingComponent)
    {
        LocalTransform parentTransform = localTransformComponentLookup[parent.Value];

        float3 liftGeneratingSurfaceGlobalPosition = liftGeneratingSurfaceLocalTransform.TransformPoint(parentTransform.Position);

        if (liftGeneratingSurfaceComponent.lastGlobalPosition.Equals(float3.zero)) liftGeneratingSurfaceComponent.lastGlobalPosition = liftGeneratingSurfaceGlobalPosition;
        
        float differenceMeters = Vector3.Distance(liftGeneratingSurfaceGlobalPosition, liftGeneratingSurfaceComponent.lastGlobalPosition);

        float metersPerSecond = differenceMeters / deltaTime;

        float liftCoefficient = fixedWingLiftComponent.pitchLiftCurve.Evaluate(fixedWingComponent.angleOfAttack) * liftGeneratingSurfaceComponent.PitchAoALiftCoefficientPercentageCurve.Evaluate(fixedWingComponent.angleOfAttack);

        float liftForce = 0.5f * AirDensity.GetAirDensityFromMeters(liftGeneratingSurfaceGlobalPosition.y) * (metersPerSecond * metersPerSecond) * liftGeneratingSurfaceComponent.liftArea * liftCoefficient;

        liftGeneratingSurfaceComponent.calculatedLiftForce = liftForce;

        liftGeneratingSurfaceComponent.lastGlobalPosition = liftGeneratingSurfaceGlobalPosition;
    }

    public static float Magnitude(float3 vector) { return (float)math.sqrt(vector.x * vector.x + vector.y * vector.y + vector.z * vector.z); }

    public static float3 Normalize(float3 value)
    {
        float mag = Magnitude(value);
        if (mag > 0.00001F)
            return value / mag;
        else
            return float3.zero;
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

    public void Execute(Entity entity, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in FixedWingComponent fixedWingComponent)
    {
        foreach (Entity liftGeneratingEntity in fixedWingComponent.liftGeneratingSurfaceEntities)
        {
            LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent = liftGeneratingSurfaceComponentLookup[liftGeneratingEntity];

            LocalTransform liftGeneratingSurfaceLocalTransform = localTransformComponentLookup[liftGeneratingEntity];

            Debug.Log(((Quaternion)liftGeneratingSurfaceLocalTransform.InverseTransformRotation(localTransformComponentLookup[entity].Rotation)).eulerAngles);

            physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, liftGeneratingSurfaceLocalTransform.Up() * liftGeneratingSurfaceComponent.calculatedLiftForce * deltaTime, liftGeneratingSurfaceLocalTransform.Position);
        }
    }
}