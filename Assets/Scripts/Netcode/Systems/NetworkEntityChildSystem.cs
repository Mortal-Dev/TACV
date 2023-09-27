using Riptide;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using static Unity.Physics.CompoundCollider;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(NetworkEntitySyncSystem))]
public partial struct NetworkEntityChildSystem : ISystem
{
    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        if (networkManagerEntityComponent.NetworkType != NetworkType.Host || networkManagerEntityComponent.NetworkType == NetworkType.Server) return;

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach (var (networkedParentRequest, entity) in SystemAPI.Query<NetworkedParentRequestComponent>().WithEntityAccess())
        {
            entityCommandBuffer.RemoveComponent<NetworkedParentRequestComponent>(entity);

            NetworkedEntityComponent networkedEntityParentComponent = SystemAPI.GetComponent<NetworkedEntityComponent>(networkedParentRequest.rootNewParent);

            NetworkedEntityComponent networkedEntityChildComponent = SystemAPI.GetComponent<NetworkedEntityComponent>(entity);

            Entity networkedEntityParent = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityParentComponent.networkEntityId);

            Entity networkedEntityChild = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(networkedEntityChildComponent.networkEntityId);
            Entity childOfParentNetworkedEntity = networkedParentRequest.newParentChildId == 0 ? networkedEntityParent : NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetChildNetworkedEntity(networkedEntityParentComponent.networkEntityId, networkedParentRequest.newParentChildId);

            Debug.Log(childOfParentNetworkedEntity.ToString());

            if (!CheckParentEntity(networkedEntityParent, in networkedEntityParentComponent)) return;

            SetNetworkedParent(childOfParentNetworkedEntity, networkedEntityChild, entityCommandBuffer);

            RemovePhysicsComponent(networkedEntityChild, entityCommandBuffer);

            Debug.Log("sending parent message to clients");

            Message message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerSetNetworkParent);
            message.Add(networkedEntityChildComponent.networkEntityId);
            message.Add(networkedEntityParentComponent.networkEntityId);
            message.Add(networkedParentRequest.newParentChildId);
            NetworkManager.Instance.Network.SendMessage(message, SendMode.Server);
        }

        entityCommandBuffer.Playback(systemState.EntityManager);
        entityCommandBuffer.Dispose();
    }

    private void RemovePhysicsComponent(Entity entity, EntityCommandBuffer entityCommandBuffer)
    {
        entityCommandBuffer.RemoveComponent<PhysicsMass>(entity);
        entityCommandBuffer.RemoveComponent<PhysicsVelocity>(entity);
        entityCommandBuffer.RemoveComponent<PhysicsWorldIndex>(entity);
        entityCommandBuffer.RemoveComponent<PhysicsCollider>(entity);
    }

    private bool CheckParentEntity(Entity networkedEntityParent, in NetworkedEntityComponent parentNetworkedEntityComponent)
    {
        if (networkedEntityParent == Entity.Null)
        {
            Debug.LogError($"attempted to create parent request to a non existent networked entity {networkedEntityParent}");
            return false;
        }

        if (!parentNetworkedEntityComponent.allowNetworkedChildrenRequests)
        {
            Debug.LogWarning($"attempted to parent to {parentNetworkedEntityComponent.networkEntityId}, but it is not allowed to have children");
            return false;
        }

        return true;
    }

    private static void SetNetworkedParent(Entity parent, Entity child, EntityCommandBuffer entityCommandBuffer)
    {
        entityCommandBuffer.AddComponent(child, new Parent { Value = parent });
        entityCommandBuffer.SetComponent(child, new LocalTransform { Position = float3.zero, Rotation = quaternion.identity, Scale = 1 });

        entityCommandBuffer.AddComponent(child, new ChildedNetworkedEntityComponent());
    }

    [MessageHandler((ushort)NetworkMessageId.ServerSetNetworkParent)]
    public static void ServerSetNetworkedParent(Message message)
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.Host) return;

        ulong childNetworkedId = message.GetULong();
        ulong parentNetworkedId = message.GetULong();
        int childId = message.GetInt();

        Entity childNetworkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(childNetworkedId);
        Entity parentNetworkedEntity = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntity(parentNetworkedId);
        Entity childOfParentNetworkedEntity = childId == 0 ? parentNetworkedEntity : NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetChildNetworkedEntity(parentNetworkedId, childId);

        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        SetNetworkedParent(childNetworkedEntity, childOfParentNetworkedEntity, entityCommandBuffer);

        entityCommandBuffer.Playback(NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager);
        entityCommandBuffer.Dispose();
    }
}