using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
public partial struct FixedWingDragSystem : ISystem
{
    float deltaTime;

    public void OnUpdate(ref SystemState systemState)
    {
        deltaTime = SystemAPI.Time.DeltaTime;

        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            foreach (var (fixedWingDragComponent, fixedWingComponent, physicsMass, physicsVelocity, localTransform) in SystemAPI.Query<RefRW<FixedWingDragComponent>, RefRW<FixedWingComponent>, RefRW<PhysicsMass>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>>().
                WithNone<UninitializedFixedWingComponent>())
            {
                UpdateDrag(fixedWingDragComponent, fixedWingComponent, physicsMass, physicsVelocity, localTransform);
            }
        }
        else
        {
            foreach (var (fixedWingDragComponent, fixedWingComponent, physicsMass, physicsVelocity, localTransform) in SystemAPI.Query<RefRW<FixedWingDragComponent>, RefRW<FixedWingComponent>, RefRW<PhysicsMass>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>>().
                WithNone<UninitializedFixedWingComponent>().WithAll<LocalOwnedNetworkedEntityComponent>())
            {
                UpdateDrag(fixedWingDragComponent, fixedWingComponent, physicsMass, physicsVelocity, localTransform);
            }
        }
    }

    private void UpdateDrag(RefRW<FixedWingDragComponent> fixedWingDragComponent, RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsMass> phyiscsMassComponent, RefRW<PhysicsVelocity> physicsVelocity, 
        RefRW<LocalTransform> localTransformComponent)
    {
        Vector3 velocity = physicsVelocity.ValueRO.Linear;

        Vector3 forward = localTransformComponent.ValueRO.Forward();
        Vector3 backward = -forward;
        Vector3 rightSide = localTransformComponent.ValueRO.Right();
        Vector3 leftSide = -rightSide;

        float altitudeMeters = localTransformComponent.ValueRO.Position.y;

        float forwardDot = Vector3.Dot(forward.normalized, velocity.normalized);
        float backwardDot = Vector3.Angle(backward.normalized, velocity.normalized);
        float rightSideDot = Vector3.Angle(rightSide.normalized, velocity.normalized);
        float leftSideDot = Vector3.Angle(leftSide.normalized, velocity.normalized);

        Vector3 oppositeVelocityNormalized = (-velocity).normalized;

        if (forwardDot > 0 && forwardDot <= 1)
        {
            float drag = CalculateDrag(velocity.magnitude, forwardDot, altitudeMeters, fixedWingDragComponent.ValueRO.forwardArea, fixedWingDragComponent.ValueRO.forwardDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);

            Debug.Log("drag: " + drag);
        }
        else if (backwardDot > 0 && backwardDot <= 1)
        {
            float drag = CalculateDrag(velocity.magnitude, backwardDot, altitudeMeters, fixedWingDragComponent.ValueRO.backArea, fixedWingDragComponent.ValueRO.backDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);
        }

        if (rightSideDot > 0 && rightSideDot <= 1)
        {
            float drag = CalculateDrag(velocity.magnitude, rightSideDot, altitudeMeters, fixedWingDragComponent.ValueRO.rightSideArea, fixedWingDragComponent.ValueRO.rightSideDragCoefficientAoACurve);

            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);
        }
        else if (leftSideDot > 0 && leftSideDot <= 1)
        {
            float drag = CalculateDrag(velocity.magnitude, leftSideDot, altitudeMeters, fixedWingDragComponent.ValueRO.leftSideArea, fixedWingDragComponent.ValueRO.leftSideDragCoefficientAoACurve);
             physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);
        }

    }

    private float CalculateDrag(float velocity, float dot, float altitudeMeters, float area, LowFidelityFixedAnimationCurve dragCurve)
    {
        float airDensity = AirDensity.GetAirDensityFromMeters(altitudeMeters);

        float dragCoefficient = dragCurve.Evaluate(1 - dot);

        float drag = dragCoefficient * airDensity * (velocity * velocity / 2f) * area;

        return drag;
    }
}