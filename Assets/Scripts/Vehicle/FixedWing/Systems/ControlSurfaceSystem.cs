using Unity.Entities;
using Unity.Transforms;

public partial struct ControlSurfaceSystem : ISystem
{
    public EntityQuery networkedEntityQuery;

    public void OnCreate(ref SystemState systemState)
    {
        networkedEntityQuery = systemState.GetEntityQuery(ComponentType.ReadOnly<LocalOwnedNetworkedEntityComponent>());


    }

    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;


    }


    public partial struct ControlSurfaceJob : IJobEntity
    {
        public void Execute(in FixedWingComponent fixedWingComponent)
        {
            
        }
    }

}
