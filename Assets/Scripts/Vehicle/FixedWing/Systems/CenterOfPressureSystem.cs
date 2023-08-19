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
public partial struct CenterOfPressureSystem : ISystem
{
    EntityQuery networkEntityQuery;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<FixedWingComponent>(), ComponentType.ReadOnly<Parent>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            new UpdateCenterOfPressureJob().ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new UpdateCenterOfPressureJob().ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    partial struct UpdateCenterOfPressureJob : IJobEntity
    {
        public void Execute(Entity entity, ref LocalTransform localTransform, in CenterOfPressureComponent centerOfPressureComponent, in Parent parent)
        {
            EntityManager entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            FixedWingComponent fixedWingComponent = entityManager.GetComponentData<FixedWingComponent>(parent.Value);

            
        }
    }
}