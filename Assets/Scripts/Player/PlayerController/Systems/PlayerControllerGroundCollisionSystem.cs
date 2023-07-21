using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Burst;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using System;

[ClientSystem]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PlayerMovementSystem))]
[BurstCompile]
public partial struct PlayerControllerGroundCollisionSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        ManagedNetworkSystems.AddManagedNetworkSystem(systemState.SystemHandle, NetworkType.Client);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.Server) return;

        if (!SystemAPI.HasSingleton<PhysicsWorldSingleton>()) return;

        foreach (var (localTransfrom, playerControllerCmponent) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerControllerComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>())
        {
            NativeList<ColliderCastHit> sphereCastColliderHits = PhysicsHelper.SphereCast(CollisionFilter.Default, localTransfrom.ValueRO.Position, -localTransfrom.ValueRO.Up(), 0.2f, 1.1f);

            //we always collide with ourselves, so if there's more, we are on the ground
            if (sphereCastColliderHits.Length > 1)
                playerControllerCmponent.ValueRW.playerState = PlayerState.Moving;
            else
                playerControllerCmponent.ValueRW.playerState = PlayerState.InAir;

            return;
        }

        foreach (var (localTransfrom, playerControllerCmponent) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerControllerComponent>>())
        {
            NativeList<ColliderCastHit> sphereCastColliderHits = PhysicsHelper.SphereCast(CollisionFilter.Default, localTransfrom.ValueRO.Position, -localTransfrom.ValueRO.Up(), 0.2f, 1.1f);

            //we always collide with ourselves, so if there's more, we are on the ground
            if (sphereCastColliderHits.Length > 1)
                playerControllerCmponent.ValueRW.playerState = PlayerState.Moving;
            else
                playerControllerCmponent.ValueRW.playerState = PlayerState.InAir;
        }
    }
}