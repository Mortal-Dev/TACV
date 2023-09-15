using Unity.Burst;
using Unity.Entities;
using Unity.Collections;

[BurstCompile]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerInputSystem))]
public partial struct FixedWingInputSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        PlayerControllerInputComponent playerControllerInputComponent = SystemAPI.GetSingleton<PlayerControllerInputComponent>();


    }

    [BurstCompile]
    partial struct FixedWingInputJob : IJobEntity
    {
        [ReadOnly] public PlayerControllerInputComponent playerControllerInputComponent;

        public void Execute(in FixedWingInputComponent fixedWingInputComponent, in LocalPlayerInVehicleComponent localPlayerInVehicleComponent)
        {
            switch (localPlayerInVehicleComponent.ownerShiptType)
            {
                case OwnershipType.Shared:

                    break;
            }
        }
    }
}
