using Unity.Entities;
using Unity.Burst;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
[BurstCompile]
public partial struct FixedWingLiftSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            foreach (var (fixedWingComponent, fixedWingLiftComponent, physicsMass, localTransform, physicsVelocity) in SystemAPI.Query<RefRO<FixedWingComponent>, RefRW<FixedWingLiftComponent>, RefRO<PhysicsMass>, 
                RefRO<LocalTransform>, RefRW<PhysicsVelocity>>())
            {
                UpdateLift(fixedWingComponent, fixedWingLiftComponent, physicsMass, localTransform, physicsVelocity, ref systemState);
            }
        }
        else
        {
            foreach (var (fixedWingComponent, fixedWingLiftComponent, physicsMass, localTransform, physicsVelocity) in SystemAPI.Query<RefRO<FixedWingComponent>, RefRW<FixedWingLiftComponent>, RefRO<PhysicsMass>, 
                RefRO<LocalTransform>, RefRW<PhysicsVelocity>>()
                .WithAll<LocalOwnedNetworkedEntityComponent>())
            {
                UpdateLift(fixedWingComponent, fixedWingLiftComponent, physicsMass, localTransform, physicsVelocity, ref systemState);
            }
        }
        
    }

    [BurstCompile]
    private void UpdateLift(RefRO<FixedWingComponent> fixedWingComponent, RefRW<FixedWingLiftComponent> fixedWingLiftComponent, RefRO<PhysicsMass> physicsMass, RefRO<LocalTransform> localTransform, 
        RefRW<PhysicsVelocity> physicsVelocity, ref SystemState systemState)
    {
        float angleOfAttackPercent = GetAoAPercent(fixedWingComponent.ValueRO.angleOfAttack);

        Debug.Log("aoa: " + fixedWingComponent.ValueRO.angleOfAttack);

        Debug.Log("angle of attack percent: " + angleOfAttackPercent);

        Debug.Log("curve evaluate test: " + fixedWingLiftComponent.ValueRO.liftCurve.Evaluate(0));

        float liftCoefficientPercent = fixedWingLiftComponent.ValueRO.liftCurve.Evaluate(angleOfAttackPercent) == 0 ? 0.02f : fixedWingLiftComponent.ValueRO.liftCurve.Evaluate(angleOfAttackPercent);

        Debug.Log("lift coefficient percent: " + liftCoefficientPercent);

        float liftCoefficient = liftCoefficientPercent / fixedWingLiftComponent.ValueRO.maxCoefficientLift;

        Debug.Log("lift coefficient: " + liftCoefficient);

        float liftPower = liftCoefficient * (AirDensity.GetAirDensityFromMeters(localTransform.ValueRO.Position.y) * 0.5f)
            * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * fixedWingLiftComponent.ValueRO.topArea;

        if (fixedWingComponent.ValueRO.angleOfAttack < 0) liftPower *= -1;

        Debug.Log("lift generatred: " + liftPower);
        Debug.Log("altitude ft: " + localTransform.ValueRO.Position.y * 3.28084f);
        Debug.Log("speed kts: " + ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * 1.943844f);

        physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, localTransform.ValueRO.Up() * liftPower * SystemAPI.Time.DeltaTime);
    }

    public float GetAoAPercent(float angleOfAttack)
    {
        return math.abs(angleOfAttack) / 90f;
    }
}