﻿using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(FixedWingStateSystem))]
public partial struct FixedWingEnginePowerSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            foreach (var (fixedWingComponent, physicsMass, physicsVelocity, localTransform) in SystemAPI.Query<RefRW<FixedWingComponent>, RefRW<PhysicsMass>, RefRW<PhysicsVelocity>, RefRO<LocalTransform>>())
                UpdateEngineThrust(fixedWingComponent, physicsMass, physicsVelocity, localTransform, ref systemState);        
        }
        else
        {
            foreach (var (fixedWingComponent, physicsMass, physicsVelocity, localTransform) in SystemAPI.Query<RefRW<FixedWingComponent>, RefRW<PhysicsMass>, RefRW<PhysicsVelocity>, RefRO<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>())
                UpdateEngineThrust(fixedWingComponent, physicsMass, physicsVelocity, localTransform, ref systemState);
        }
    }

    private void UpdateEngineThrust(RefRW<FixedWingComponent> fixedWingComponent, RefRW<PhysicsMass> physicsMass, RefRW<PhysicsVelocity> physicsVelocity, RefRO<LocalTransform> localTransform, ref SystemState systemState)
    {
        foreach (Entity engineEntity in fixedWingComponent.ValueRO.engineEntities)
        {
            EngineComponent engineComponent = SystemAPI.GetComponent<EngineComponent>(engineEntity);

            LocalTransform engineLocalTransform = SystemAPI.GetComponent<LocalTransform>(engineEntity);

            engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.ValueRO.throttle;

            physicsVelocity.ValueRW.ApplyImpulse(physicsMass.ValueRO, physicsMass.ValueRO.Transform.pos, physicsMass.ValueRO.Transform.rot, ((Vector3)localTransform.ValueRO.Forward()).normalized * engineComponent.maxAfterBurnerPowerNewtons * SystemAPI.Time.DeltaTime, engineLocalTransform.Position);

            break;
        }
    }
}