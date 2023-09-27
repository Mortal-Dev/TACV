using Riptide;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System;
using Unity.Physics;
using System.Diagnostics;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct RequestEnterVehicleSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (requestVehicleEnterComponent, entity) in SystemAPI.Query<RefRO<RequestVehicleEnterComponent>>().WithEntityAccess())
        {
            Message message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ClientRequestVehicleEnter);

            message.Add(requestVehicleEnterComponent.ValueRO.vehicleNetworkId);
            message.Add(requestVehicleEnterComponent.ValueRO.seat);

            NetworkManager.Instance.Network.SendMessage(message, SendMode.Client);

            entityCommandBuffer.RemoveComponent(entity, ComponentType.ReadWrite<RequestVehicleEnterComponent>());
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    [MessageHandler((ushort)NetworkMessageId.ClientRequestVehicleEnter)]
    public static void ServerRecieveClientRequestVehicleEnter(ushort clientId, Message message)
    {
        ulong vehicleNetworkId = message.GetULong();
        int seatPosition = message.GetInt();

        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        Entity playerEntity = EntityHelper.GetEntityWithPredicate(entityManager, ComponentType.ReadWrite<PlayerComponent>(), x => entityManager.GetComponentData<NetworkedEntityComponent>(x).connectionId == clientId);

        if (entityManager.HasComponent<InVehicleComponent>(playerEntity)) return;

        Entity vehicleEntity = EntityHelper.GetEntityWithPredicate(entityManager, ComponentType.ReadWrite<VehicleComponent>(), x => entityManager.GetComponentData<NetworkedEntityComponent>(x).networkEntityId == vehicleNetworkId);

        VehicleComponent vehicleComponent = entityManager.GetComponentData<VehicleComponent>(vehicleEntity);

        Entity seatEntity = Array.Find(vehicleComponent.seats.ToArray(), x => entityManager.GetComponentData<VehicleSeatComponent>(x).seatPosition == seatPosition);

        if (seatEntity == Entity.Null)
        {
            UnityEngine.Debug.Log($"unable to find seat {seatPosition} in client seat request");
            return;
        }

        VehicleSeatComponent vehicleSeatComponent = entityManager.GetComponentData<VehicleSeatComponent>(seatEntity);

        if (vehicleSeatComponent.isOccupied) return;

        vehicleSeatComponent.occupiedBy = playerEntity;

        entityManager.SetComponentData(seatEntity, vehicleSeatComponent);

        entityManager.AddComponentData(playerEntity, new InVehicleComponent() { seat = seatEntity, vehicle = vehicleEntity });

        Message confirmClientVehicleEnterRequest = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerConfirmClientVehicleEnterRequest).Add(clientId).Add(vehicleNetworkId).Add(seatPosition);

        NetworkManager.Instance.Network.SendMessage(confirmClientVehicleEnterRequest, SendMode.Server);

        UnityEngine.Debug.Log("setting parent request component");

        entityManager.AddComponentData(playerEntity, new NetworkedParentRequestComponent() { rootNewParent = vehicleEntity, newParentChildId = entityManager.GetComponentData<NetworkedEntityChildComponent>(seatEntity).Id });
    }

    [MessageHandler((ushort)NetworkMessageId.ServerConfirmClientVehicleEnterRequest)]
    public static void ClientRecieveServerConfirmClientRequestVehicleEnter(Message message)
    {
        int clientIdEnteringVehicle = message.GetUShort();
        ulong vehicleNetworkId = message.GetULong();
        int seatPosition = message.GetInt();

        EntityManager entityManager = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager;

        Entity playerEntity = EntityHelper.GetEntityWithPredicate(entityManager, ComponentType.ReadWrite<PlayerComponent>(), x => entityManager.GetComponentData<NetworkedEntityComponent>(x).connectionId == clientIdEnteringVehicle);

        Entity vehicleEntity = EntityHelper.GetEntityWithPredicate(entityManager, ComponentType.ReadWrite<VehicleComponent>(), x => entityManager.GetComponentData<NetworkedEntityComponent>(x).networkEntityId == vehicleNetworkId);

        VehicleComponent vehicleComponent = entityManager.GetComponentData<VehicleComponent>(vehicleEntity);

        Entity seatEntity = Array.Find(vehicleComponent.seats.ToArray(), x => entityManager.GetComponentData<VehicleSeatComponent>(x).seatPosition == seatPosition);

        VehicleSeatComponent vehicleSeatComponent = entityManager.GetComponentData<VehicleSeatComponent>(vehicleComponent.seats[seatPosition]);

        if (NetworkManager.Instance.NetworkType != NetworkType.Host) entityManager.AddComponentData(playerEntity, new InVehicleComponent() { seat = seatEntity, vehicle = vehicleEntity });

        vehicleSeatComponent.occupiedBy = playerEntity;
    }
}