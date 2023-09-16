using Unity.Entities;

partial struct SeatComponent : IComponentData
{
    public int id;

    public bool isOccupied;

    public Entity occupiedBy;

    public bool hasOwnership;

    public bool hasOwnershipCapability;
}