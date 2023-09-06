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

        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
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
        Vector3 globalVelocity = physicsVelocity.ValueRO.Linear;

        float angleOfAttack = fixedWingComponent.ValueRO.angleOfAttack;
        float yawAngleOfAttack = fixedWingComponent.ValueRO.angleOfAttackYaw;

        float altitudeMeters = localTransformComponent.ValueRO.Position.y;

        Vector3 oppositeVelocityNormalized = (-globalVelocity).normalized;

        Debug.Log(yawAngleOfAttack);

        if (angleOfAttack >= -90 && angleOfAttack <= 90)
        {
            float drag = CalculateDrag(globalVelocity.magnitude, angleOfAttack, altitudeMeters, fixedWingDragComponent.ValueRO.forwardArea, fixedWingDragComponent.ValueRO.forwardDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized * (1 - (math.abs(yawAngleOfAttack) / 90)));
            Vector3 test = deltaTime * drag * oppositeVelocityNormalized;
            Debug.Log("front drag: " + test.magnitude);
        }
        else
        {
            float drag = CalculateDrag(globalVelocity.magnitude, angleOfAttack, altitudeMeters, fixedWingDragComponent.ValueRO.backArea, fixedWingDragComponent.ValueRO.backDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized * (1 - (math.abs(yawAngleOfAttack) / 90)));
            Vector3 test = deltaTime * drag * oppositeVelocityNormalized;
            Debug.Log("back drag: " + test.magnitude);
        }

        if (yawAngleOfAttack < 0)
        {
            float drag = CalculateDrag(globalVelocity.magnitude, yawAngleOfAttack, altitudeMeters, fixedWingDragComponent.ValueRO.rightSideArea, fixedWingDragComponent.ValueRO.rightSideDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);
            Vector3 test = deltaTime * drag * oppositeVelocityNormalized;
            Debug.Log("right side drag: " + test.magnitude);
        }
        else
        {
            float drag = CalculateDrag(globalVelocity.magnitude, yawAngleOfAttack, altitudeMeters, fixedWingDragComponent.ValueRO.leftSideArea, fixedWingDragComponent.ValueRO.leftSideDragCoefficientAoACurve);
            physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, deltaTime * drag * oppositeVelocityNormalized);
            Vector3 test = deltaTime * drag * oppositeVelocityNormalized;
            Debug.Log("left side drag: " + test.magnitude);
        }
       
    }

    private float CalculateDrag(float velocity, float angleOfAttack, float altitudeMeters, float area, LowFidelityFixedAnimationCurve dragCurve)
    {
        float airDensity = AirDensity.GetAirDensityFromMeters(altitudeMeters);

        float dragCoefficient = dragCurve.Evaluate(angleOfAttack * 180);

        float drag = dragCoefficient * airDensity * (velocity * velocity / 2f) * area;

        return drag;
    }
}