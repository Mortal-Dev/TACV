using Unity.Entities;
using UnityEngine;
using Unity.Transforms;
using Unity.Physics;
using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Jobs;
using System.Runtime.InteropServices;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
[UpdateBefore(typeof(FixedWingLiftSystem))]
[BurstCompile]
public partial struct CenterOfLiftPositionSystem : ISystem
{
    EntityQuery networkEntityQuery;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<FixedWingComponent>(), ComponentType.ReadOnly<Parent>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new UpdateCenterOfPressureJob() { entityManager = systemState.EntityManager }.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new UpdateCenterOfPressureJob() { entityManager = systemState.EntityManager }.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    partial struct UpdateCenterOfPressureJob : IJobEntity
    {
        [ReadOnly] public EntityManager entityManager;

        public void Execute(Entity entity, ref LocalTransform localTransform, in CenterOfPressureComponent centerOfPressureComponent, in Parent parent)
        {
            FixedWingComponent fixedWingComponent = entityManager.GetComponentData<FixedWingComponent>(parent.Value);   
        }
    }
}