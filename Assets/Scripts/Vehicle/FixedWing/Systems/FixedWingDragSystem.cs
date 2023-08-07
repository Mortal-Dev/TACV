using UnityEngine;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlanePhysicsSystem))]
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

        float3 forward = localTransformComponent.ValueRO.Forward();
        float3 backward = -forward;
        float3 rightSide = localTransformComponent.ValueRO.Right();
        float3 leftSide = -rightSide;

        float altitudeMeters = localTransformComponent.ValueRO.Position.y;

        float forwardAngle = Vector3.Angle(forward, velocity);
        float backwardAngle = Vector3.Angle(backward, velocity);
        float rightSideAngle = Vector3.Angle(rightSide, velocity);
        float leftSideAngle = Vector3.Angle(leftSide, velocity);

        Vector3 oppositeVelocity = (-velocity).normalized;

        if (forwardAngle > 0 && forwardAngle < 90)
        {
            float drag = CalculateDrag(velocity, forward, altitudeMeters, fixedWingDragComponent.ValueRO.forwardArea, fixedWingDragComponent.ValueRO.forwardDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, oppositeVelocity * drag * deltaTime);
        }
        else if (backwardAngle > 0 && backwardAngle < 90)
        {
            float drag = CalculateDrag(velocity, backward, altitudeMeters, fixedWingDragComponent.ValueRO.backArea, fixedWingDragComponent.ValueRO.backDragCoefficientAoACurve);
            Debug.Log(backwardAngle);

           // physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, oppositeVelocity * drag * deltaTime);
        }

        if (rightSideAngle > 0 && rightSideAngle < 89)
        {
            float drag = CalculateDrag(velocity, rightSide, altitudeMeters, fixedWingDragComponent.ValueRO.rightSideArea, fixedWingDragComponent.ValueRO.rightSideDragCoefficientAoACurve);
            Debug.Log(rightSideAngle);

            //physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, oppositeVelocity * drag * deltaTime);
        }
        else if (leftSideAngle > 0 && leftSideAngle < 89)
        {
            float drag = CalculateDrag(velocity, leftSide, altitudeMeters, fixedWingDragComponent.ValueRO.leftSideArea, fixedWingDragComponent.ValueRO.leftSideDragCoefficientAoACurve);
            Debug.Log(leftSideAngle);
            // physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, oppositeVelocity * drag * deltaTime);
        }

        physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, localTransformComponent.ValueRW.Forward() * 49000 * 2 * deltaTime);

        Debug.Log("speed kts: " + velocity.magnitude * 1.94);
    }

    private float CalculateDrag(float3 velocity, float3 direction, float altitudeMeters, float area, FixedAnimationCurve dragCurve)
    {
        float angle = Vector3.Angle(direction, velocity);

        if (angle <= 0 || angle > 90) return 0f;

        float airDensity = AirDensity.GetAirDensityFromMeters(altitudeMeters);

        float aoaPercent = angle / 90f;

        float dragCoefficient = dragCurve.Evaluate(aoaPercent);

        float drag = dragCoefficient * airDensity * (((Vector3)velocity).magnitude * ((Vector3)velocity).magnitude / 2f) * area;

        return drag;
    }
}