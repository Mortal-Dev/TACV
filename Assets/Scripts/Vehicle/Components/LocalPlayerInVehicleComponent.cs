using Unity.Entities;

public enum OwnershipType
{
    Shared,
    Owned,
    NotOwned
}

public partial struct LocalPlayerInVehicleComponent : IComponentData
{
    public OwnershipType ownerShiptType;
}
