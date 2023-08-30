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

    public void OnCreate(ref SystemState systemState)
    {
        updateliftNetworkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<LiftGeneratingSurfaceComponent>(), ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<Parent>(),
            ComponentType.ReadOnly<NetworkedEntityChildComponent>());

        applyLiftNetworkedEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<PhysicsVelocity>(), ComponentType.ReadOnly<PhysicsMass>(), ComponentType.ReadOnly<FixedWingComponent>(), 
            ComponentType.ReadOnly<FixedWingLiftComponent>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        fixedWingLiftComponentLookup = systemState.GetComponentLookup<FixedWingLiftComponent>();

        localTransfromComponentLookup = systemState.GetComponentLookup<LocalTransform>();

        liftGeneratingSurfaceComponentLookup = systemState.GetComponentLookup<LiftGeneratingSurfaceComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        fixedWingLiftComponentLookup.Update(ref systemState);
        localTransfromComponentLookup.Update(ref systemState);
        liftGeneratingSurfaceComponentLookup.Update(ref systemState);

        UpdateLiftJob updateLiftJob = new() { deltaTime = SystemAPI.Time.DeltaTime, fixedWingLiftComponentLookup = fixedWingLiftComponentLookup, localTransformComponentLookup = localTransfromComponentLookup };
        ApplyLiftJob applyLiftJob = new() { deltaTime = SystemAPI.Time.DeltaTime, liftGeneratingSurfaceComponentLookup = liftGeneratingSurfaceComponentLookup };

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

    public void Execute([ChunkIndexInQuery] int sortKey, ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform localTransform, in Parent parent)
    {
        if (!fixedWingLiftComponentLookup.TryGetComponent(parent.Value, out FixedWingLiftComponent fixedWingLiftComponent)) throw new System.Exception("unable to find fixedwinglift component in parent");
        
        ProcessPitchLift(ref liftGeneratingSurfaceComponent, ref localTransform, in parent, fixedWingLiftComponent);
    }

    [BurstCompile]
    private void ProcessPitchLift(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform liftGeneratingSurfaceLocalTransform,
        in Parent parent, FixedWingLiftComponent fixedWingLiftComponent)
    {
        LocalTransform parentTransform = localTransformComponentLookup[parent.Value];

        float3 liftGeneratingSurfaceGlobalPosition = liftGeneratingSurfaceLocalTransform.TransformPoint(parentTransform.Position);

        if (liftGeneratingSurfaceComponent.lastGlobalPosition.Equals(float3.zero)) liftGeneratingSurfaceComponent.lastGlobalPosition = liftGeneratingSurfaceGlobalPosition;
        
        float differenceMeters = Vector3.Distance(liftGeneratingSurfaceGlobalPosition, liftGeneratingSurfaceComponent.lastGlobalPosition);

        float metersPerSecond = differenceMeters / deltaTime;

        float pitchAngleOfAttack = 0;

        float pitchLiftCoefficient = fixedWingLiftComponent.pitchLiftCurve.Evaluate(pitchAngleOfAttack) * liftGeneratingSurfaceComponent.PitchAoALiftCoefficientPercentageCurve.Evaluate(pitchAngleOfAttack);

        float liftPower = 0.5f * AirDensity.GetAirDensityFromMeters(liftGeneratingSurfaceGlobalPosition.y) * (metersPerSecond * metersPerSecond) * 40 * pitchLiftCoefficient;

        liftGeneratingSurfaceComponent.calculatedLiftForce = Normalize(liftGeneratingSurfaceLocalTransform.Up()) * liftPower;

        liftGeneratingSurfaceComponent.lastLocalPosition = liftGeneratingSurfaceLocalTransform.Position;

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
    [ReadOnly] public ComponentLookup<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponentLookup;

    [ReadOnly] public float deltaTime;

    public void Execute(Entity entity, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass, in FixedWingComponent fixedWingComponent, in FixedWingLiftComponent fixedWingLiftComponent)
    {
        foreach (Entity liftGeneratingEntity in fixedWingComponent.liftGeneratingSurfaceEntities)
        {
            LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent = liftGeneratingSurfaceComponentLookup[liftGeneratingEntity];

            physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, liftGeneratingSurfaceComponent.calculatedLiftForce, liftGeneratingSurfaceComponent.lastLocalPosition);
        }
    }
}