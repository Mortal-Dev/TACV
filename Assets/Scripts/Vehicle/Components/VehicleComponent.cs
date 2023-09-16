using Unity.Entities;
using Unity.Collections;

public partial struct VehicleComponent : IComponentData
{
    public FixedList128Bytes<Entity> seats;

    public Entity seatWithOwnership;

    public int vehicleId;
}
