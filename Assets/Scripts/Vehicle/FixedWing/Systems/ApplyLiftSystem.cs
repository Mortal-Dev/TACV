using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;

/*[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingLiftSystem))]
[BurstCompile]
public partial struct ApplyLiftSystem : ISystem
{
    EntityQuery networkEntityQuery;

    ComponentLookup<LiftGeneratingSurfaceComponent> liftGeneratingSurfaceComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<PhysicsVelocity>(), ComponentType.ReadOnly<PhysicsMass>(), ComponentType.ReadOnly<FixedWingComponent>(), ComponentType.ReadOnly<FixedWingLiftComponent>(),
            ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());

        liftGeneratingSurfaceComponentLookup = systemState.GetComponentLookup<LiftGeneratingSurfaceComponent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        liftGeneratingSurfaceComponentLookup.Update(ref systemState);

        ApplyLiftJob applyLiftJob = new ApplyLiftJob() { liftGeneratingSurfaceComponentLookup = liftGeneratingSurfaceComponentLookup, deltaTime = SystemAPI.Time.DeltaTime };

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            applyLiftJob.ScheduleParallel(systemState.Dependency);
        }
        else
        {
            applyLiftJob.ScheduleParallel(networkEntityQuery, systemState.Dependency);
        }

    }
}*/

