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
    public void OnUpdate(ref SystemState systemState)
    {

        /*if (NetworkManager.Instance.NetworkType == NetworkType.None)
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
        }*/

        foreach (var (fixedWingDragComponent, physicsMass, physicsVelocity, localTransform) in SystemAPI.Query<RefRW<FixedWingDragComponent>, RefRW<PhysicsMass>, RefRW<PhysicsVelocity>, RefRW<LocalTransform>>().
                WithNone<UninitializedFixedWingComponent>())
        {
            UpdateDrag(fixedWingDragComponent, default, physicsMass, physicsVelocity, localTransform);
        }
    }

    private void UpdateDrag(RefRW<FixedWingDragComponent> fixedWingDragComponent, RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsMass> phyiscsMassComponent, RefRW<PhysicsVelocity> physicsVelocity, 
        RefRW<LocalTransform> localTransformComponent)
    {
        var invRotation = Quaternion.Inverse(localTransformComponent.ValueRO.Rotation);

        float3 localVelocity = invRotation * physicsVelocity.ValueRO.Linear;

        float3 forward = localTransformComponent.ValueRO.Forward();
        float3 backward = -forward;
        float3 upSide = localTransformComponent.ValueRO.Up();
        float3 downSide = -upSide;
        float3 rightSide = localTransformComponent.ValueRO.Right();
        float3 leftSide = -rightSide;

        physicsVelocity.ValueRW.ApplyLinearImpulse(phyiscsMassComponent.ValueRO, localTransformComponent.ValueRO.Forward() * 0.2f);

        Debug.Log(upSide);
        Debug.Log(Vector3.Dot(upSide, localVelocity));
    }
}