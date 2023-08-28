﻿using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
[BurstCompile]
public partial struct FixedWingLiftSystem : ISystem
{
    EntityQuery networkEntityQuery;

    ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<LiftGeneratingSurfaceComponent>(), ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<Parent>(),
            ComponentType.ReadOnly<NetworkedEntityChildComponent>());

        fixedWingComponentLookup = systemState.GetComponentLookup<FixedWingComponent>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        fixedWingComponentLookup.Update(ref systemState);

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new UpdateLiftJob() { fixedWingComponentLookup = fixedWingComponentLookup }.ScheduleParallel(systemState.Dependency);
        }
        else
        {
            new UpdateLiftJob() { fixedWingComponentLookup = fixedWingComponentLookup }.ScheduleParallel(networkEntityQuery, systemState.Dependency);
        }

    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    partial struct UpdateLiftJob : IJobEntity
    {
        public ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

        public void Execute(Entity entity, ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform localTransform, in Parent parent)
        {
            if (!fixedWingComponentLookup.TryGetComponent(parent.Value, out FixedWingComponent fixedWingComponent))
            {
                Debug.LogError("attempting to put lift on a non fixed wing, make sure all lift gameobject/entities are direct children of the fixed wing");
                return;
            }
        }

        private void ProcessPitchLift(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform localTransform, in Parent parent, 
            EntityManager entityManager)
        {
            FixedWingLiftComponent fixedWingLiftComponent = entityManager.GetComponentData<FixedWingLiftComponent>(liftGeneratingSurfaceComponent.liftEntity);
        }
    }

    /*private void UpdateLift(RefRO<FixedWingComponent> fixedWingComponent, RefRW<FixedWingLiftComponent> fixedWingLiftComponent, RefRO<PhysicsMass> physicsMass, RefRO<LocalTransform> localTransform, 
        RefRW<PhysicsVelocity> physicsVelocity, ref SystemState systemState)
    {
        float angleOfAttack = fixedWingComponent.ValueRO.angleOfAttack;

        float angleOfAttackPercent = (angleOfAttack + 90f) / 180f;

        Vector3 forward = localTransform.ValueRO.Forward();

        float liftCoefficientPercent = fixedWingLiftComponent.ValueRO.liftCurve.Evaluate(angleOfAttackPercent);

        float liftCoefficient = (fixedWingLiftComponent.ValueRO.maxCoefficientLift - fixedWingLiftComponent.ValueRO.minCoefficientLift) * liftCoefficientPercent + fixedWingLiftComponent.ValueRO.minCoefficientLift;

        float liftPower = liftCoefficient * (AirDensity.GetAirDensityFromMeters(localTransform.ValueRO.Position.y) * 0.5f)
            * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * fixedWingLiftComponent.ValueRO.topArea;

        float3 centerOfPressure = SystemAPI.GetComponent<LocalTransform>(fixedWingComponent.ValueRO.centerOfPressureEntity).Position;

        //physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, RotateVectorByX(vec3.normalized, 90f) * liftPower * SystemAPI.Time.DeltaTime);

        Debug.Log("up vector: " + localTransform.ValueRO.Up());
        Debug.Log("lift vector: " + RotateVectorByX(forward, 90f));

     //   physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, localTransform.ValueRO.Up() * liftPower * SystemAPI.Time.DeltaTime);
       // physicsVelocity.ValueRW.ApplyImpulse(physicsMass.ValueRO, physicsMass.ValueRO.Transform.pos, physicsMass.ValueRO.Transform.rot, localTransform.ValueRO.Up() * liftPower * SystemAPI.Time.DeltaTime, centerOfPressure);

        Debug.Log("altitude ft: " + localTransform.ValueRO.Position.y * 3.28084f);
        Debug.Log("speed kts: " + ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * 1.943844f);
    }*/

    // Function to rotate a Vector3 by a specified angle around the X-axis
    private float3 RotateVectorByX(float3 vector, float angle)
    {
        angle *= -1f;

        float radianAngle = angle * Mathf.Deg2Rad;
        float sinAngle = math.sin(radianAngle);
        float cosAngle = math.cos(radianAngle);

        float3 rotatedVector = new float3(
            vector.x,
            vector.y * cosAngle - vector.z * sinAngle,
            vector.y * sinAngle + vector.z * cosAngle
        );

        return rotatedVector;
    }
}