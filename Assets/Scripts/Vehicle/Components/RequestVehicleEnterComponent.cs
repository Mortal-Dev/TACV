using Unity.Entities;

public partial struct RequestVehicleEnterComponent : IComponentData
{
    public int vehicleNetworkId;
    public int seat;
}