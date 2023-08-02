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
        deltaTime = ((FixedStepSimulationSystemGroup)systemState.World.GetExistingSystemManaged(typeof(FixedStepSimulationSystemGroup))).Timestep;

        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            foreach (var (fixedWingComponent, localTransform, velocity, physicsMass) in SystemAPI.Query<RefRW<FixedWingComponent>, RefRO<LocalTransform>,
            RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithNone<UninitializedFixedWingComponent>())
            {
                UpdateFixedWing(fixedWingComponent, localTransform, velocity, physicsMass, ref systemState);
            }
        }
        else
        {
            foreach (var (fixedWingComponent, localTransform, velocity, physicsMass) in SystemAPI.Query<RefRW<FixedWingComponent>, RefRO<LocalTransform>,
            RefRW<PhysicsVelocity>, RefRW<PhysicsMass>>().WithNone<UninitializedFixedWingComponent>().WithAll<LocalOwnedNetworkedEntityComponent>())
            {
                UpdateFixedWing(fixedWingComponent, localTransform, velocity, physicsMass, ref systemState);
            }
        }
    }

    private void UpdateFixedWing(RefRW<FixedWingComponent> fixedWingComponent, RefRO<LocalTransform> localTransformComponent, 
        RefRW<PhysicsVelocity> physicsVelocityComponent, RefRW<PhysicsMass> physicsMassComponent, ref SystemState systemState)
    {
        inverseRotation = Quaternion.Inverse(localTransformComponent.ValueRO.Rotation);

        SetGForce(fixedWingComponent, physicsVelocityComponent);

        CalculatePhysics(localTransformComponent, physicsVelocityComponent, fixedWingComponent);

        ApplyThrust(fixedWingComponent, physicsVelocityComponent, physicsMassComponent, localTransformComponent, ref systemState);
    }

    private void CalculatePhysics(RefRO<LocalTransform> localTransform, RefRW<PhysicsVelocity> physicsVelocity, RefRW<FixedWingComponent> fixedWingComponent)
    {
        var invRotation = Quaternion.Inverse(localTransform.ValueRO.Rotation);

        fixedWingComponent.ValueRW.localVelocity = invRotation * physicsVelocity.ValueRO.Linear;  //transform world velocity into local space
        fixedWingComponent.ValueRW.localAngularVelocity = invRotation * physicsVelocity.ValueRO.Angular;  //transform into local space
            
        SetAngleOfAttack(fixedWingComponent);
    }

    private void SetAngleOfAttack(RefRW<FixedWingComponent> fixedWingComponent)
    {
        if (((Vector3)fixedWingComponent.ValueRO.localVelocity).sqrMagnitude < 0.1f)
        {
            fixedWingComponent.ValueRW.angleOfAttack = 0;
            fixedWingComponent.ValueRW.angleOfAttackYaw = 0;
            return;
        }

        fixedWingComponent.ValueRW.angleOfAttack = math.atan2(-fixedWingComponent.ValueRO.localVelocity.y, fixedWingComponent.ValueRO.localVelocity.z);
        fixedWingComponent.ValueRW.angleOfAttackYaw = math.atan2(fixedWingComponent.ValueRO.localVelocity.x, fixedWingComponent.ValueRO.localVelocity.z);
    }
    
    private void SetGForce(RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsVelocity> physicsVelocity)
    {
        float3 acceleration = (physicsVelocity.ValueRO.Linear - fixedWingComponent.ValueRO.lastVelocity) / deltaTime;

        fixedWingComponent.ValueRW.lastVelocity = physicsVelocity.ValueRO.Linear;

        fixedWingComponent.ValueRW.gForce = inverseRotation * acceleration;
    }

    private void ApplyThrust(RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsVelocity> physicsVelocity, RefRW<PhysicsMass> physicsMass, RefRO<LocalTransform> localTransform, ref SystemState systemState)
    {
        physicsVelocity.ValueRW.ApplyLinearImpulse(in physicsMass.ValueRO, localTransform.ValueRO.Forward() * GetCurrentEngineThrust(fixedWingComponent, ref systemState));
    }

    private float GetCurrentEngineThrust(RefRW<FixedWingComponent> fixedWingComponent, ref SystemState systemState)
    {
        float total = 0f;

        foreach (Entity entity in fixedWingComponent.ValueRO.engineEntities)
        {
            total += SystemAPI.GetComponent<EngineComponent>(entity).currentPower;
        }

        return total;
    }

    void UpdateDrag(RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsVelocity> physicsVelocity, RefRW<PhysicsMass> physicsMass)
    {
        var localVelocity = fixedWingComponent.ValueRO.localVelocity;
        var localVelocitySquared = ((Vector3)localVelocity).sqrMagnitude;

        //calculate coefficient of drag depending on direction on velocity
        var coefficient = Scale6(
            ((Vector3)localVelocity).normalized,
            fixedWingComponent.ValueRO.rightSideDrag.Evaluate(math.abs(localVelocity.x)), fixedWingComponent.ValueRO.leftSideDrag.Evaluate(math.abs(localVelocity.x)),
            fixedWingComponent.ValueRO.topDrag.Evaluate(math.abs(localVelocity.y)), fixedWingComponent.ValueRO.beneathDrag.Evaluate(math.abs(localVelocity.y)),
            fixedWingComponent.ValueRO.frontDrag.Evaluate(math.abs(localVelocity.z)),
            fixedWingComponent.ValueRO.backDrag.Evaluate(math.abs(localVelocity.z))
        );

        var drag = ((Vector3)coefficient).magnitude * localVelocitySquared * -((Vector3)localVelocity).normalized;    //drag is opposite direction of velocity

        //physicsVelocity.ValueRW.ApplyImpulse(physicsMass.ValueRO, physicsMass.ValueRO.)
    }

    private float3 Scale6(
        float3 value,
        float posX, float negX,
        float posY, float negY,
        float posZ, float negZ
    )
    {
        float3 result = value;

        if (result.x > 0)
        {
            result.x *= posX;
        }
        else if (result.x < 0)
        {
            result.x *= negX;
        }

        if (result.y > 0)
        {
            result.y *= posY;
        }
        else if (result.y < 0)
        {
            result.y *= negY;
        }

        if (result.z > 0)
        {
            result.z *= posZ;
        }
        else if (result.z < 0)
        {
            result.z *= negZ;
        }

        return result;
    }
}
