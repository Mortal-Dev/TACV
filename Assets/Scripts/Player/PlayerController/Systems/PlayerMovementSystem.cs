using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.Collections;
using System;
using UnityEngine;
using Unity.Jobs;
using Unity.Physics.Systems;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct PlayerMovementSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        SetPlayerRotation(ref systemState);

        foreach (var (playerController, entity) in SystemAPI.Query<RefRW<PlayerControllerComponent>>().WithAll<PlayerControllerInputComponent>().WithAll<Simulate>().WithEntityAccess())
        {
            switch (playerController.ValueRO.playerState)
            {
                case PlayerState.Moving:
                    PlayerControllerMoving(playerController,
                        SystemAPI.GetComponentRW<PhysicsVelocity>(entity),
                        SystemAPI.GetComponentRW<LocalTransform>(entity),
                        SystemAPI.GetComponentRO<PlayerControllerInputComponent>(entity));
                    break;

                case PlayerState.InAir:
                    PlayerControllerInAir(playerController,
                        SystemAPI.GetComponentRW<PhysicsVelocity>(entity));
                    break;
            }
        }
    }

    [BurstCompile]
    private void PlayerControllerMoving(RefRW<PlayerControllerComponent> playerController, RefRW<PhysicsVelocity> physicsVelocity, RefRW<LocalTransform> localTransform, RefRO<PlayerControllerInputComponent> input)
    {
        //forwards/backwards rw
        physicsVelocity.ValueRW.Linear += localTransform.ValueRO.Forward() * input.ValueRO.leftControllerThumbstick.y * (input.ValueRO.leftControllerThumbstick.y > 0 ? playerController.ValueRO.forwardForce : playerController.ValueRO.backwardForce);

        //left/right //rw
        physicsVelocity.ValueRW.Linear += localTransform.ValueRO.Right() * input.ValueRO.leftControllerThumbstick.x * playerController.ValueRO.sideForce;

        //cap velocities
        Vector3 velocityVector3 = physicsVelocity.ValueRO.Linear;
        
        if (input.ValueRO.leftControllerThumbstick.y > 0 && velocityVector3.magnitude > playerController.ValueRO.maxForwardVelocity)
            physicsVelocity.ValueRW.Linear = velocityVector3.normalized * playerController.ValueRO.maxForwardVelocity;
        else if (input.ValueRO.leftControllerThumbstick.y < 0 && velocityVector3.magnitude > playerController.ValueRO.maxBackwardsVelocity)
            physicsVelocity.ValueRW.Linear = velocityVector3.normalized * playerController.ValueRO.maxBackwardsVelocity;
        else if (input.ValueRO.leftControllerThumbstick.x != 0 && velocityVector3.magnitude > playerController.ValueRO.maxSideVelocity)
            physicsVelocity.ValueRW.Linear = velocityVector3.normalized * playerController.ValueRO.maxSideVelocity;

        physicsVelocity.ValueRW.Linear = velocityVector3;

        //remove angular velocity
        physicsVelocity.ValueRW.Angular = float3.zero;
    }

    [BurstCompile]
    private void PlayerControllerInAir(RefRW<PlayerControllerComponent> playerController, RefRW<PhysicsVelocity> physicsVelocity)
    {
        if (physicsVelocity.ValueRO.Linear.y > playerController.ValueRO.maxDownVelocity)
            physicsVelocity.ValueRW.Linear = playerController.ValueRO.maxDownVelocity;
    }

    [BurstCompile]
    private void SetPlayerRotation(ref SystemState systemState)
    {
        foreach (var (characterControllerLocalTransform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PlayerControllerComponent>().WithEntityAccess())
        {
            DynamicBuffer<LinkedEntityGroup> dynamicLinkedEntityGroupBuffer = systemState.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

            foreach (LinkedEntityGroup linkedEntityGroup in dynamicLinkedEntityGroupBuffer)
            {
                if (!SystemAPI.HasComponent<HeadComponent>(linkedEntityGroup.Value)) continue;

                RefRO<LocalTransform> headLocalTransform = SystemAPI.GetComponentRO<LocalTransform>(entity);

                characterControllerLocalTransform.ValueRW.Position = headLocalTransform.ValueRO.Position;
                characterControllerLocalTransform.ValueRW.Rotation = headLocalTransform.ValueRO.Rotation;
            }
        }
    }
}