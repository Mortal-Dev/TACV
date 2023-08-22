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

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PlayerMovementSystem))]
[BurstCompile]
public partial struct PlayerControllerGroundCollisionSystem : ISystem
{
    EntityQuery networkedEntityQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        networkedEntityQuery = systemState.GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadWrite<PlayerControllerComponent>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntity)) return;

        if (!SystemAPI.HasSingleton<PhysicsWorldSingleton>()) return;

        if (networkManagerEntity.NetworkType == NetworkType.None)
        {
            new PlayerControllerGroundCollisionJob().ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new PlayerControllerGroundCollisionJob().ScheduleParallel(networkedEntityQuery, systemState.Dependency).Complete();
        }

       /* foreach (var (localTransfrom, playerControllerCmponent) in SystemAPI.Query<RefRO<LocalTransform>, RefRW<PlayerControllerComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>())
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
        }*/
    }

    [BurstCompile]
    partial struct PlayerControllerGroundCollisionJob : IJobEntity
    {
        public void Execute(in LocalTransform localTransform, ref PlayerControllerComponent playerControllerComponent)
        {
            NativeList<ColliderCastHit> sphereCastColliderHits = PhysicsHelper.SphereCast(CollisionFilter.Default, localTransform.Position, -localTransform.Up(), 0.2f, 1.1f);

            //we always collide with ourselves, so if there's more, we are on the ground
            if (sphereCastColliderHits.Length > 1)
                playerControllerComponent.playerState = PlayerState.Moving;
            else
                playerControllerComponent.playerState = PlayerState.InAir;
        }
    }
}