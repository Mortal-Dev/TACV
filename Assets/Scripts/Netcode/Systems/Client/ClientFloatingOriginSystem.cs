using Unity.Entities;
using Unity.Burst;

[DisableAutoCreation]
[UpdateBefore(typeof(TickSystem))]
[ClientSystem]
[BurstCompile]
public partial struct ClientFloatingOriginSystem : ISystem
{

}