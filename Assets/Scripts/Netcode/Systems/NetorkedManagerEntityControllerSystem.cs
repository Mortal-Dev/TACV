using Unity.Entities;

[UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
public partial struct NetorkedManagerEntityControllerSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        if (SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent))
        {
            networkManagerEntityComponent.NetworkType = NetworkManager.Instance.NetworkType;
        }
        else
        {
            Entity networkManagerEntity = systemState.EntityManager.CreateEntity();
            systemState.EntityManager.AddComponentData(networkManagerEntity, new NetworkManagerEntityComponent() { NetworkType = NetworkManager.Instance.NetworkType });
        }
    }
}