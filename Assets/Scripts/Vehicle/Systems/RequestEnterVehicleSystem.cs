using Riptide;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using System.Linq;
using System;
using Unity.Physics;

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

        if (clientIdEnteringVehicle == NetworkManager.CLIENT_NET_ID) entityManager.AddComponentData(playerEntity, new FakeChildComponent() { parent = seatEntity });

        VehicleSeatComponent vehicleSeatComponent = entityManager.GetComponentData<VehicleSeatComponent>(vehicleComponent.seats[seatPosition]);

        ComponentStorage.AddComponentToStorage(playerEntity, entityManager.GetComponentData<PhysicsVelocity>(playerEntity));

        entityManager.RemoveComponent<PhysicsVelocity>(playerEntity);
        entityManager.RemoveComponent<PhysicsCollider>(playerEntity);
        entityManager.RemoveComponent<PhysicsDamping>(playerEntity);
        entityManager.RemoveComponent<PhysicsMass>(playerEntity);
        entityManager.RemoveComponent<PhysicsWorldIndex>(playerEntity);

        if (NetworkManager.Instance.NetworkType != NetworkType.Host) entityManager.AddComponentData(playerEntity, new InVehicleComponent() { seat = seatEntity, vehicle = vehicleEntity });

        vehicleSeatComponent.occupiedBy = playerEntity;
    }
}