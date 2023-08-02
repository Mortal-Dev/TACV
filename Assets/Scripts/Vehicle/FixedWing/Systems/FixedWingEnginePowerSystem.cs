using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PlanePhysicsSystem))]
public partial struct FixedWingEnginePowerSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
        {
            foreach (RefRW<FixedWingComponent> fixedWingComponent in SystemAPI.Query<RefRW<FixedWingComponent>>())
                UpdateEngineThrust(fixedWingComponent, ref systemState);        
        }
        else
        {
            foreach (RefRW<FixedWingComponent> fixedWingComponent in SystemAPI.Query<RefRW<FixedWingComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>())
                UpdateEngineThrust(fixedWingComponent, ref systemState);
        }
    }

    private void UpdateEngineThrust(RefRW<FixedWingComponent> fixedWingComponent, ref SystemState systemState)
    {
        foreach (Entity engineEntity in fixedWingComponent.ValueRO.engineEntities)
        {
            EngineComponent engineComponent = SystemAPI.GetComponent<EngineComponent>(engineEntity);

            engineComponent.currentPower = engineComponent.maxAfterBurnerPowerNewtons * fixedWingComponent.ValueRO.throttle;
        }
    }
}