using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Riptide;

[DisableAutoCreation]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[NetworkSystemBase]
[BurstCompile]
public partial struct NetworkEntitySyncSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        foreach (var (localTransformRecord, localTransform, networkedEntityComponent) in SystemAPI.Query<RefRW<PreviousLocalTransformRecordComponent>, RefRO<LocalTransform>, RefRO<NetworkedEntityComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>())
        {
            if (localTransform.ValueRO.Equals(localTransformRecord.ValueRO.localTransformRecord)) return;

            localTransformRecord.ValueRW.localTransformRecord = localTransform.ValueRO;

            Message message = Message.Create(MessageSendMode.Unreliable, NetworkMessageId.SyncEntities);

            message.Add(networkedEntityComponent.ValueRO.networkEntityId);
            message.AddLocalTransform(localTransform.ValueRO);

            SendMode sendMode = NetworkManager.Instance.NetworkType == NetworkType.Host || NetworkManager.Instance.NetworkType == NetworkType.Server ? SendMode.Server : SendMode.Client;

            NetworkManager.Instance.Network.SendMessage(message, sendMode);
        }
    }
}