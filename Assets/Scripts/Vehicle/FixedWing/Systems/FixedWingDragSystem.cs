using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Burst;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
[BurstCompile]
public partial struct FixedWingDragSystem : ISystem
{
    float deltaTime;

    EntityQuery networkEntityQuery;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<FixedWingDragComponent>(), ComponentType.ReadWrite<FixedWingComponent>(), ComponentType.ReadWrite<PhysicsMass>(), 
            ComponentType.ReadWrite<PhysicsVelocity>(), ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<NetworkedEntityComponent>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        deltaTime = SystemAPI.Time.DeltaTime;

        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        FixedWingDragJob fixedWingDragJob = new FixedWingDragJob() { deltaTime = SystemAPI.Time.DeltaTime };

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            fixedWingDragJob.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            fixedWingDragJob.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }
    
    [BurstCompile]
    partial struct FixedWingDragJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;

        public readonly void Execute(ref FixedWingDragComponent fixedWingDragComponent, ref FixedWingComponent fixedWingComponent, ref PhysicsVelocity physicsVelocity, in PhysicsMass physicsMass,
            in LocalTransform localTransform)
        {
            Vector3 globalVelocity = physicsVelocity.Linear;

            float angleOfAttack = fixedWingComponent.angleOfAttack;
            float yawAngleOfAttack = fixedWingComponent.angleOfAttackYaw;

            float altitudeMeters = localTransform.Position.y;

            Vector3 oppositeVelocityNormalized = (-globalVelocity).normalized;

            if (angleOfAttack >= -90 && angleOfAttack <= 90)
            {
                float drag = CalculateDrag(globalVelocity.magnitude, angleOfAttack, altitudeMeters, fixedWingDragComponent.forwardArea, fixedWingDragComponent.forwardDragCoefficientAoACurve);
                physicsVelocity.ApplyLinearImpulse(physicsMass, (1 - (math.abs(yawAngleOfAttack) / 90)) * deltaTime * drag * oppositeVelocityNormalized);
            }
            else
            {
                float drag = CalculateDrag(globalVelocity.magnitude, angleOfAttack, altitudeMeters, fixedWingDragComponent.backArea, fixedWingDragComponent.backDragCoefficientAoACurve);
                physicsVelocity.ApplyLinearImpulse(physicsMass, (1 - (math.abs(yawAngleOfAttack) / 90)) * deltaTime * drag * oppositeVelocityNormalized);
            }

            if (yawAngleOfAttack > 1)
            {
                float drag = CalculateDrag(globalVelocity.magnitude, yawAngleOfAttack, altitudeMeters, fixedWingDragComponent.rightSideArea, fixedWingDragComponent.rightSideDragCoefficientAoACurve);
                physicsVelocity.ApplyLinearImpulse(physicsMass, deltaTime * drag * oppositeVelocityNormalized);
            }
            else if (yawAngleOfAttack < -1)
            {
                float drag = CalculateDrag(globalVelocity.magnitude, yawAngleOfAttack, altitudeMeters, fixedWingDragComponent.leftSideArea, fixedWingDragComponent.leftSideDragCoefficientAoACurve);
                physicsVelocity.ApplyLinearImpulse(physicsMass, deltaTime * drag * oppositeVelocityNormalized);
            }
        }

        [BurstCompile]
        private readonly float CalculateDrag(float velocity, float angleOfAttack, float altitudeMeters, float area, LowFidelityFixedAnimationCurve dragCurve)
        {
            float airDensity = AirDensity.GetAirDensityFromMeters(altitudeMeters);

            float dragCoefficient = dragCurve.Evaluate(angleOfAttack / 180);

            float drag = dragCoefficient * airDensity * (velocity * velocity / 2f) * area;

            return drag;
        }
    }
}