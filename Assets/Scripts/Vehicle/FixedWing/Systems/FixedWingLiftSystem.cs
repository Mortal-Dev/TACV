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
            ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

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

        LocalTransform liftGeneratingSurfaceGlobalTransform = liftGeneratingSurfaceLocalTransform.TransformTransform(parentTransform);

        if (liftGeneratingSurfaceComponent.lastGlobalTransform.Position.Equals(float3.zero)) liftGeneratingSurfaceComponent.lastGlobalTransform = liftGeneratingSurfaceGlobalTransform;
        
        float differenceMeters = Vector3.Distance(liftGeneratingSurfaceGlobalTransform.Position, liftGeneratingSurfaceComponent.lastGlobalTransform.Position);

        float metersPerSecond = differenceMeters / deltaTime;

        float liftCoefficient = fixedWingLiftComponent.pitchLiftCurve.Evaluate(fixedWingComponent.angleOfAttack) * liftGeneratingSurfaceComponent.PitchAoALiftCoefficientPercentageCurve.Evaluate(fixedWingComponent.angleOfAttack);

        float liftForce = 0.5f * AirDensity.GetAirDensityFromMeters(liftGeneratingSurfaceGlobalTransform.Position.y) * (metersPerSecond * metersPerSecond) * liftGeneratingSurfaceComponent.liftArea * liftCoefficient;

        liftGeneratingSurfaceComponent.calculatedLiftForce = liftForce;

        liftGeneratingSurfaceComponent.lastLocalPosition = liftGeneratingSurfaceLocalTransform.Position;

        liftGeneratingSurfaceComponent.lastGlobalTransform = liftGeneratingSurfaceGlobalTransform;

        Debug.Log("lift force: " + liftGeneratingSurfaceComponent.calculatedLiftForce);
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

    public void Execute(ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in FixedWingComponent fixedWingComponent, in LocalTransform localTransform)
    {
        Debug.Log("speed kts: " + ((Vector3)physicsVelocity.Linear).magnitude * 1.943844f);

        foreach (Entity liftGeneratingEntity in fixedWingComponent.liftGeneratingSurfaceEntities)
        {
            LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent = liftGeneratingSurfaceComponentLookup[liftGeneratingEntity];

            LocalTransform liftGeneratingSurfaceLocalTransform = localTransformComponentLookup[liftGeneratingEntity];

            physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, MathHelper.Normalize(localTransform.TransformDirection(Quaternion.Euler(new float3(-90, 0, 0)) * fixedWingComponent.localVelocity)) * liftGeneratingSurfaceComponent.calculatedLiftForce * deltaTime, liftGeneratingSurfaceLocalTransform.Position);
        }
    }

    static float3 RotateVectorAroundX(float3 vector, float degreesToRotate)
    {
        return Quaternion.Euler(degreesToRotate, 0, 0) * vector;
    }
}