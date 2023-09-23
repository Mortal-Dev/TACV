using UnityEngine;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct VehicleInteractSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        foreach (var (playerComponent, localOwnedNetworkedEntityComponent, entity) in SystemAPI.Query<RefRO<PlayerComponent>, RefRO<LocalOwnedNetworkedEntityComponent>>()
            .WithNone<InVehicleComponent>().WithNone<RequestVehicleEnterComponent>().WithEntityAccess())
        {
            Debug.Log("found player");

            foreach (RefRO<NetworkedEntityComponent> networkedEntityComponent in SystemAPI.Query<RefRO<NetworkedEntityComponent>>().WithAll<VehicleComponent>())
            {
                Debug.Log("added request component");
                systemState.EntityManager.AddComponentData(entity, new RequestVehicleEnterComponent() { seat = 0, vehicleNetworkId = networkedEntityComponent.ValueRO.networkEntityId });
            }
        }
    }
}