using UnityEngine;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct VehicleInteractSystem : ISystem
{
    bool didRequest;

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (didRequest) return;

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (playerComponent, localOwnedNetworkedEntityComponent, entity) in SystemAPI.Query<RefRO<PlayerComponent>, RefRO<LocalOwnedNetworkedEntityComponent>>()
            .WithNone<InVehicleComponent>().WithNone<RequestVehicleEnterComponent>().WithEntityAccess())
        {
            foreach (RefRO<NetworkedEntityComponent> networkedEntityComponent in SystemAPI.Query<RefRO<NetworkedEntityComponent>>().WithAll<VehicleComponent>())
            {
                entityCommandBuffer.AddComponent(entity, new RequestVehicleEnterComponent() { seat = 0, vehicleNetworkId = networkedEntityComponent.ValueRO.networkEntityId });

                didRequest = true;
            }
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }
}