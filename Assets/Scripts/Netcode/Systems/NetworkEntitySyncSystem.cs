using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
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
        foreach (var (localTransformRecord, localTransform, networkedEntityComponent, entity) in SystemAPI.Query<RefRW<PreviousLocalTransformRecordComponent>, RefRO<LocalTransform>, RefRO<NetworkedEntityComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithEntityAccess())
        {
            //don't send a sync to ourself if we're host
            if (NetworkManager.Instance.NetworkType == NetworkType.Host && networkedEntityComponent.ValueRO.connectionId == NetworkManager.CLIENT_NET_ID) continue;

            if (localTransform.ValueRO.Equals(localTransformRecord.ValueRO.localTransformRecord) || 
                GetDirtyNetworkedChildrenComponents(SystemAPI.GetBuffer<LinkedEntityGroup>(entity), ref systemState, out List<NetworkedEntityChildLocalTransform> dirtyNetworkedChildren)) continue;

            localTransformRecord.ValueRW.localTransformRecord = localTransform.ValueRO;

            SendNetworkedEntitySyncMessage(networkedEntityComponent.ValueRO.networkEntityId, localTransform.ValueRO, dirtyNetworkedChildren);
        }
    }

    [BurstCompile]
    private bool GetDirtyNetworkedChildrenComponents(DynamicBuffer<LinkedEntityGroup> linkedEntityGroupBuffer, ref SystemState systemState, out List<NetworkedEntityChildLocalTransform> dirtyNetworkedChildren)
    {
        List<NetworkedEntityChildLocalTransform> networkedEntityChildComponents = new List<NetworkedEntityChildLocalTransform>();

        foreach (LinkedEntityGroup linkedEntityGroup in linkedEntityGroupBuffer)
        {
            if (!SystemAPI.HasComponent<NetworkedEntityChildComponent>(linkedEntityGroup.Value)) continue;

            Entity entity = linkedEntityGroup.Value;

            NetworkedEntityChildComponent networkedEntityChildComponent = SystemAPI.GetComponent<NetworkedEntityChildComponent>(entity);
            LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(entity);
            PreviousLocalTransformRecordComponent previousLocalTransformRecordComponent = SystemAPI.GetComponent<PreviousLocalTransformRecordComponent>(entity);

            if (previousLocalTransformRecordComponent.localTransformRecord.Equals(localTransform)) continue;

            previousLocalTransformRecordComponent.localTransformRecord = localTransform;

            SystemAPI.SetComponent(entity, previousLocalTransformRecordComponent);

            networkedEntityChildComponents.Add(new NetworkedEntityChildLocalTransform() { NetworkedEntityChildComponent = networkedEntityChildComponent, LocalTransform = localTransform } );
        }

        dirtyNetworkedChildren = networkedEntityChildComponents;

        return networkedEntityChildComponents.Count == 0;
    }

    private void SendNetworkedEntitySyncMessage(ulong id, LocalTransform localTransform, List<NetworkedEntityChildLocalTransform> networkedEntityChildComponents)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, NetworkManager.Instance.NetworkType == NetworkType.Host || NetworkManager.Instance.NetworkType == NetworkType.Server ? NetworkMessageId.ServerSyncEntity : NetworkMessageId.ClientSyncOwnedEntities);

        message.Add(id);

        networkedEntityChildComponents.Insert(0, new NetworkedEntityChildLocalTransform() { LocalTransform = localTransform, NetworkedEntityChildComponent = new NetworkedEntityChildComponent { childEntityMap = new FixedList128Bytes<short>() { -1 } } });

        message.Add(networkedEntityChildComponents.Count);

        foreach (NetworkedEntityChildLocalTransform networkedEntityChildComponent in networkedEntityChildComponents)
        {
            message.AddLocalTransform(networkedEntityChildComponent.LocalTransform);
            message.Add(networkedEntityChildComponent.NetworkedEntityChildComponent.childEntityMap.ToArray());
        }
        
        SendMode sendMode = NetworkManager.Instance.NetworkType == NetworkType.Host || NetworkManager.Instance.NetworkType == NetworkType.Server ? SendMode.Server : SendMode.Client;

        NetworkManager.Instance.Network.SendMessage(message, sendMode);
    }
}

public struct NetworkedEntityChildLocalTransform
{
    public NetworkedEntityChildComponent NetworkedEntityChildComponent;

    public LocalTransform LocalTransform;
}