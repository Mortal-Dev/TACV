using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct VehicleSeatInitializeSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (vehicleComponent, localTransform, entity) in SystemAPI.Query<RefRW<VehicleComponent>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            SetSeats(vehicleComponent, localTransform, entity, ref systemState);
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    private void SetSeats(RefRW<VehicleComponent> vehicleComponent, RefRW<LocalTransform> localTransform, Entity entity, ref SystemState systemState)
    {
        DynamicBuffer<LinkedEntityGroup> childBuffer = systemState.EntityManager.GetBuffer<LinkedEntityGroup>(entity);

        FixedList128Bytes<Entity> seats = new FixedList128Bytes<Entity>();

        foreach (LinkedEntityGroup childEntityGoup in childBuffer)
        {
            if (!systemState.EntityManager.HasComponent<VehicleSeatComponent>(entity)) return;

            seats.Add(childEntityGoup.Value);
        }

        EntityManager entityManager = systemState.EntityManager;

        seats.OrderBy(entity => entityManager.GetComponentData<VehicleSeatComponent>(entity).seatPosition);

        vehicleComponent.ValueRW.seats = seats;
    }
}