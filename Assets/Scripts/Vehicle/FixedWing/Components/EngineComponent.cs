using Unity.Entities;

public partial struct EngineComponent : IComponentData
{
    public float maxMilitaryPowerNewtons;

    public float maxAfterBurnerPowerNewtons;

    public float currentPower;
}