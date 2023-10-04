using Riptide;
using System;
using System.Linq;
using Unity.Burst;
using Unity.Entities;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct VehicleLeaveSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (vehicleLeaveComponent, entity) in SystemAPI.Query<RefRO<RequestVehicleLeaveComponent>>().WithEntityAccess())
        {
            Message message = Message.Create(MessageSendMode.Reliable, ClientToServerNetworkMessageId.ClientRequestVehicleLeave);

            message.Add(vehicleLeaveComponent.ValueRO.vehicleNetworkId);
            message.Add(vehicleLeaveComponent.ValueRO.seat);

            NetworkManager.Instance.Network.SendMessage(message, SendMode.Client);

            entityCommandBuffer.RemoveComponent<RequestVehicleLeaveComponent>(entity);
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    [MessageHandler((ushort)ClientToServerNetworkMessageId.ClientRequestVehicleLeave)]
    public static void ServerRecieveClientRequestVehicleLeave(ushort clientId, Message message)
    {
        ulong vehicleNetworkId = message.GetULong();
        int seatPosition = message.GetInt();

        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        Entity playerEntity = EntityHelper.GetEntityWithPredicate(entityManager, ComponentType.ReadWrite<PlayerComponent>(), x => entityManager.GetComponentData<NetworkedEntityComponent>(x).connectionId == clientId);

        var (vehicleSeatEntity, vehicleSeatComponent) = VehicleHelper.GetVehicleSeatEntity(vehicleNetworkId, seatPosition);

        var (vehicleEntity, vehicleComponent) = VehicleHelper.GetVehicleEntity(vehicleNetworkId);

        if (vehicleSeatComponent.occupiedBy != playerEntity) return;

        SendMessage();

        void SendMessage()
        {
            Message leaveMessage = Message.Create(MessageSendMode.Reliable, ServerToClientNetworkMessageId.ServerConfirmClientVehicleLeaveRequest);

            leaveMessage.Add(entityManager.GetComponentData<NetworkedEntityComponent>(vehicleEntity).networkEntityId);
            leaveMessage.Add(seatPosition);

            NetworkManager.Instance.Network.SendMessage(leaveMessage, SendMode.Server);
        }
    }

    [MessageHandler((ushort)ServerToClientNetworkMessageId.ServerConfirmClientVehicleLeaveRequest)]
    public static void ClientRecieveServerConfirmClientRequestVehicleLeave(Message message)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.Host) return;

        ulong vehicleNetworkId = message.GetULong();
        int seatPosition = message.GetInt();

        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        var (vehicleEntity, vehicleComponent) = VehicleHelper.GetVehicleEntity(vehicleNetworkId);

        var (seatEntity, seatComponent) = VehicleHelper.GetVehicleSeatEntity(in vehicleComponent, seatPosition);

        entityManager.AddComponentData(seatComponent.occupiedBy, new NetworkedUnparentRequestComponent() { rootParent = vehicleEntity });

        seatComponent.occupiedBy = Entity.Null;

        entityManager.SetComponentData(seatEntity, seatComponent);

        if (vehicleComponent.currentSeatWithOwnership != seatEntity) return;

        vehicleComponent.currentSeatWithOwnership = Entity.Null;

        entityManager.SetComponentData(vehicleEntity, vehicleComponent);
    }
}