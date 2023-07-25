using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Extensions;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct PlanePhysicsSystem : ISystem
{
    private float deltaTime;

    private Quaternion inverseRotation;

    public void OnUpdate(ref SystemState systemState)
    {
        deltaTime = 1f / NetworkManager.TICKS_PER_SECOND;

        foreach (var (fixedWingComponent, localTransform, velocity, physicsMass, entity) in SystemAPI.Query<RefRW<FixedWingComponent>, RefRO<LocalTransform>, RefRW<PhysicsVelocity>, 
            RefRW<PhysicsMass>>().WithEntityAccess())
        {
            inverseRotation = Quaternion.Inverse(localTransform.ValueRO.Rotation);

            fixedWingComponent.ValueRW.gForce = CalculateGForce(localTransform, fixedWingComponent, velocity);

            velocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, fixedWingComponent.ValueRO.throttle * 117000 * localTransform.ValueRO.Forward());
        }
    }

    private void CalculateLocalVelocities(RefRW<PhysicsVelocity> physicsVelocity)
    {
        
    }

    private float3 CalculateGForce(RefRO<LocalTransform> localTransform, RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsVelocity> physicsVelocity)
    {
        float3 acceleration = (physicsVelocity.ValueRO.Linear - fixedWingComponent.ValueRO.lastVelocity) / deltaTime;

        fixedWingComponent.ValueRW.lastVelocity = physicsVelocity.ValueRO.Linear;

        return inverseRotation * acceleration;
    }
}
