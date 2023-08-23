using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using System.Linq;
using Riptide;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
public partial struct NetworkEntitySyncSystem : ISystem
{
    private NetworkManagerEntityComponent networkManagerEntityComponent;

    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out networkManagerEntityComponent)) return;
        
        if (networkManagerEntityComponent.NetworkType == NetworkType.None) return;

        foreach (var (localTransformRecord, localTransform, networkedEntityComponent, entity) in SystemAPI.Query<RefRW<PreviousLocalTransformRecordComponent>, RefRO<LocalTransform>, RefRO<NetworkedEntityComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithEntityAccess())
        {
            bool updateLocalTransformOfParentEntity = false;

            if (!localTransformRecord.ValueRO.localTransformRecord.Equals(localTransform.ValueRO))
            {
                updateLocalTransformOfParentEntity = true;
                localTransformRecord.ValueRW.localTransformRecord = localTransform.ValueRO;
            }

            DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(entity);

            FixedList512Bytes<NetworkedEntityChildrenMap> finalChangedChildrenMap = new FixedList512Bytes<NetworkedEntityChildrenMap>();

            foreach (Child child in children)
            {
                FixedList512Bytes<NetworkedEntityChildrenMap> changedChildrenMaps = GetChangedChildrenPaths(child, ref systemState);

                foreach (NetworkedEntityChildrenMap changedChildMap in changedChildrenMaps) finalChangedChildrenMap.Add(changedChildMap);
            }

            SendSyncMessage(networkedEntityComponent.ValueRO.networkEntityId, localTransform.ValueRO, updateLocalTransformOfParentEntity, finalChangedChildrenMap);
        }
    }

    private FixedList512Bytes<NetworkedEntityChildrenMap> GetChangedChildrenPaths(Child root, ref SystemState systemState)
    {
        FixedList512Bytes<NetworkedEntityChildrenMap> result = new FixedList512Bytes<NetworkedEntityChildrenMap>();

        FindChangedChildren(root, ref result, new NetworkedEntityChildrenMap(), ref systemState);

        return result;
    }

    private void FindChangedChildren(Child root, ref FixedList512Bytes<NetworkedEntityChildrenMap> result, NetworkedEntityChildrenMap current, ref SystemState systemState)
    {
        if (!SystemAPI.HasComponent<NetworkedEntityChildComponent>(root.Value)) return;

        NetworkedEntityChildComponent networkedEntityComponent = SystemAPI.GetComponent<NetworkedEntityChildComponent>(root.Value);
        PreviousLocalTransformRecordComponent previousLocalTransformRecordComponent = SystemAPI.GetComponent<PreviousLocalTransformRecordComponent>(root.Value);
        LocalTransform localTransform = SystemAPI.GetComponent<LocalTransform>(root.Value);

        current.NetworkedEntityChildMap.Add(networkedEntityComponent.Id);

        if (!localTransform.Equals(previousLocalTransformRecordComponent.localTransformRecord))
        {
            previousLocalTransformRecordComponent.localTransformRecord = localTransform;
            SystemAPI.SetComponent(root.Value, previousLocalTransformRecordComponent);
            current.LocalTransform = localTransform;
            result.Add(current);
        }


        DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(root.Value);

        foreach (Child child in children)
        {
            FindChangedChildren(child, ref result, new NetworkedEntityChildrenMap() { NetworkedEntityChildMap = current.NetworkedEntityChildMap }, ref systemState );
        }

        return;
    }

    private void SendSyncMessage(ulong parentNetworkedEntityId, LocalTransform parentNetworkedEntityTransform, bool updateNetworkedEntityTransform, FixedList512Bytes<NetworkedEntityChildrenMap> changedChildMap)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)(networkManagerEntityComponent.NetworkType == NetworkType.Server || networkManagerEntityComponent.NetworkType == NetworkType.Host ? NetworkMessageId.ServerSyncEntity : NetworkMessageId.ClientSyncOwnedEntities));

        message.Add(parentNetworkedEntityId);

        message.Add(updateNetworkedEntityTransform);

        //child might update, but parent might not
        if (updateNetworkedEntityTransform) message.AddLocalTransform(parentNetworkedEntityTransform);

        message.Add(changedChildMap.Length);

        foreach (NetworkedEntityChildrenMap networkedEntityChildMapLocalTransform in changedChildMap)
        {
            message.AddInts(networkedEntityChildMapLocalTransform.NetworkedEntityChildMap.ToArray());
            message.AddLocalTransform(networkedEntityChildMapLocalTransform.LocalTransform);
        }

        NetworkManager.Instance.Network.SendMessage(message, NetworkManager.Instance.NetworkType == NetworkType.Server || NetworkManager.Instance.NetworkType == NetworkType.Host ? SendMode.Server : SendMode.Client);
    }

}

partial struct NetworkedEntityChildrenMap
{
    public FixedList512Bytes<int> NetworkedEntityChildMap;

    public LocalTransform LocalTransform;
}