using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct VehicleSeatInitializeSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (uninitializedVehicleComponent, localTransform, entity) in SystemAPI.Query<RefRW<UninitializedVehicleComponent>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            VehicleComponent vehicleComponent = SetSeats(entity, ref systemState);

            entityCommandBuffer.RemoveComponent<UninitializedVehicleComponent>(entity);
            entityCommandBuffer.AddComponent(entity, vehicleComponent);
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    private VehicleComponent SetSeats(Entity entity, ref SystemState systemState)
    {
        DynamicBuffer<LinkedEntityGroup> childBuffer = systemState.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

        FixedList128Bytes<Entity> seats = new FixedList128Bytes<Entity>();

        VehicleComponent vehicleComponent = new VehicleComponent();

        foreach (LinkedEntityGroup childEntityGoup in childBuffer)
        {
            if (!systemState.EntityManager.HasComponent<VehicleSeatComponent>(childEntityGoup.Value)) continue;

            seats.Add(childEntityGoup.Value);
        }

        EntityManager entityManager = systemState.EntityManager;

        seats.OrderBy(entity => entityManager.GetComponentData<VehicleSeatComponent>(entity).seatPosition);

        vehicleComponent.seats = seats;

        return vehicleComponent;
    }
}