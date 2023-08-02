using Unity.Entities;

public partial struct RudderComponent : IComponentData
{
    public float currentRudderAngleDegrees;

    public float currentRudderDrag;

    public float maxRudderAngleDegrees;

    public float maxRudderDrag;
}
