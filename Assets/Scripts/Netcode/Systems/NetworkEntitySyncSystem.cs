using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using Riptide;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[BurstCompile]
public partial struct NetworkEntitySyncSystem : ISystem
{

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None) return;

        foreach (var (localTransformRecord, localTransform, networkedEntityComponent, entity) in SystemAPI.Query<RefRW<PreviousLocalTransformRecordComponent>, RefRO<LocalTransform>, RefRO<NetworkedEntityComponent>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithEntityAccess())
        {
            bool updateLocalTransformOfParentEntity = false;

            if (!localTransformRecord.ValueRO.localTransformRecord.Equals(localTransform.ValueRO))
            {
                updateLocalTransformOfParentEntity = true;
                localTransformRecord.ValueRW.localTransformRecord = localTransform.ValueRO;
            }

            DynamicBuffer<Child> children = SystemAPI.GetBuffer<Child>(entity);

            List<NetworkedEntityChildMapLocalTransform> finalChangedChildrenMap = new List<NetworkedEntityChildMapLocalTransform>();

            foreach (Child child in children)
            {
                List<NetworkedEntityChildMapLocalTransform> changedChildrenMaps = PathChangedChildren(child, ref systemState);

                foreach (NetworkedEntityChildMapLocalTransform changedChildMap in changedChildrenMaps) finalChangedChildrenMap.Add(changedChildMap);
            }

            SendSyncMessage(networkedEntityComponent.ValueRO.networkEntityId, localTransform.ValueRO, updateLocalTransformOfParentEntity, finalChangedChildrenMap);
        }
    }

    private List<NetworkedEntityChildMapLocalTransform> PathChangedChildren(Child root, ref SystemState systemState)
    {
        List<NetworkedEntityChildMapLocalTransform> result = new List<NetworkedEntityChildMapLocalTransform>();

        FindChangedChildren(root, result, new NetworkedEntityChildMapLocalTransform(), ref systemState);

        return result;
    }

    private void FindChangedChildren(Child root, List<NetworkedEntityChildMapLocalTransform> result, NetworkedEntityChildMapLocalTransform current, ref SystemState systemState)
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
            FindChangedChildren(child, result, new NetworkedEntityChildMapLocalTransform() { NetworkedEntityChildMap = new List<int>(current.NetworkedEntityChildMap) }, ref systemState );
        }

        return;
    }

    private void SendSyncMessage(ulong parentNetworkedEntityId, LocalTransform parentNetworkedEntityTransform, bool updateNetworkedEntityTransform, List<NetworkedEntityChildMapLocalTransform> changedChildMap)
    {
        Message message = Message.Create(MessageSendMode.Unreliable, (ushort)(NetworkManager.Instance.NetworkType == NetworkType.Server || NetworkManager.Instance.NetworkType == NetworkType.Host ? NetworkMessageId.ServerSyncEntity : NetworkMessageId.ClientSyncOwnedEntities));

        message.Add(parentNetworkedEntityId);

        message.Add(updateNetworkedEntityTransform);

        //child might update, but parent might not
        if (updateNetworkedEntityTransform) message.AddLocalTransform(parentNetworkedEntityTransform);

        message.Add(changedChildMap.Count);

        foreach (NetworkedEntityChildMapLocalTransform networkedEntityChildMapLocalTransform in changedChildMap)
        {
            message.AddInts(networkedEntityChildMapLocalTransform.NetworkedEntityChildMap.ToArray());
            message.AddLocalTransform(networkedEntityChildMapLocalTransform.LocalTransform);
        }

        NetworkManager.Instance.Network.SendMessage(message, NetworkManager.Instance.NetworkType == NetworkType.Server || NetworkManager.Instance.NetworkType == NetworkType.Host ? SendMode.Server : SendMode.Client);
    }

}

class NetworkedEntityChildMapLocalTransform
{
    public List<int> NetworkedEntityChildMap;

    public LocalTransform LocalTransform;

    public NetworkedEntityChildMapLocalTransform()
    {
        NetworkedEntityChildMap = new List<int>();
    }
}