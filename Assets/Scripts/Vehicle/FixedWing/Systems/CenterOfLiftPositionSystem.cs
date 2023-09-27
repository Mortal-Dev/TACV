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

    ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<CenterOfPressureComponent>(), ComponentType.ReadOnly<Parent>(), ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        fixedWingComponentLookup = systemState.GetComponentLookup<FixedWingComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        fixedWingComponentLookup.Update(ref systemState);

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new UpdateCenterOfPressureJob() { fixedWingComponentLookup = fixedWingComponentLookup }.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new UpdateCenterOfPressureJob() { fixedWingComponentLookup = fixedWingComponentLookup }.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }
    }

    [BurstCompile]
    [StructLayout(LayoutKind.Auto)]
    partial struct UpdateCenterOfPressureJob : IJobEntity
    {
        [ReadOnly] public ComponentLookup<FixedWingComponent> fixedWingComponentLookup;

        public void Execute(Entity entity, ref LocalTransform localTransform, in CenterOfPressureComponent centerOfPressureComponent, in Parent parent)
        {
            RefRO<FixedWingComponent> fixedWingComponent = fixedWingComponentLookup.GetRefRO(parent.Value);
        }
    }
}