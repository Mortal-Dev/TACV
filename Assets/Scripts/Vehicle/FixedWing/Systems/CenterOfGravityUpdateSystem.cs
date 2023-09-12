using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct CenterOfGravityUpdateSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        foreach (var (localTransform, parent) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<Parent>>().WithAll<CenterOfGravityComponent>())
        {
            RefRW<PhysicsMass> physicsMass = SystemAPI.GetComponentRW<PhysicsMass>(parent.ValueRO.Value);

            physicsMass.ValueRW.CenterOfMass = new Unity.Mathematics.float3(0, physicsMass.ValueRW.CenterOfMass.y, physicsMass.ValueRW.CenterOfMass.z);
        }
    }
}