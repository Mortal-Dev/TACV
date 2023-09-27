using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.XR.CoreUtils;
using Unity.Physics.Systems;
using Unity.Mathematics;

[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SyncLocalPlayerToXROriginSystem : SystemBase
{
    public GameObject XROriginGameObject;

    public GameObject localHeadGameObject;
    public GameObject localLeftHandGameObject;
    public GameObject localRightHandGameObject;

    bool foundLocalVRObjects = true;

    protected override void OnUpdate()
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.Server) return;

        if (localHeadGameObject == null || localLeftHandGameObject == null || localRightHandGameObject == null)
        {
            SetXRGameObjects();
            return;
        }

        SetPositionOfXROriginToEntity();

        SetPositionRotationOfLocalTransform(GetEntityLeftHandTransform(), localLeftHandGameObject.transform);

        SetPositionRotationOfLocalTransform(GetEntityRightHandTransform(), localRightHandGameObject.transform);

        SetPositionRotationOfLocalTransform(GetEntityHeadTransform(), localHeadGameObject.transform);

        foundLocalVRObjects = true;
    }

    private void SetPositionRotationOfLocalTransform(RefRW<LocalTransform> localTransform, Transform transform)
    {
        if (transform == null || !foundLocalVRObjects) return;

        transform.GetLocalPositionAndRotation(out Vector3 position, out Quaternion rotation);

        localTransform.ValueRW.Position = position;
        localTransform.ValueRW.Rotation = rotation;
    }

    private void SetPositionOfXROriginToEntity()
    {
        foreach (var (localTransform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithAll<PlayerComponent>().WithEntityAccess())
        {
            Entity rootEntity = entity;

            bool hasParent = SystemAPI.HasComponent<Parent>(entity);

            float3 newPosition = localTransform.ValueRO.Position;
            Quaternion newRotation = localTransform.ValueRO.Rotation;

            int iterator = 0;

            while (hasParent && iterator < 4)
            {
                Entity parent = SystemAPI.GetComponent<Parent>(rootEntity).Value;

                LocalTransform parentTransform = SystemAPI.GetComponent<LocalTransform>(parent);

                LocalTransform childTransform = SystemAPI.GetComponent<LocalTransform>(rootEntity);

                newPosition = childTransform.TransformPoint(float3.zero);
                newRotation = childTransform.TransformRotation(quaternion.identity);

                rootEntity = parent;

                hasParent = SystemAPI.HasComponent<Parent>(rootEntity);

                iterator++;
            }

            XROriginGameObject.transform.position = SystemAPI.GetComponent<LocalTransform>(rootEntity).TransformPoint(newPosition);
            XROriginGameObject.transform.rotation = SystemAPI.GetComponent<LocalTransform>(rootEntity).TransformRotation(newRotation);
        }
    }

    private void SetXRGameObjects()
    {
        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();

        if (xrOrigin == null)
        {
            return;
        }

        XROriginGameObject = xrOrigin.gameObject;

        GameObject cameraOffset = xrOrigin.gameObject.GetNamedChild("Camera Offset");

        foreach (Transform transform in cameraOffset.transform)
        {
            Debug.Log(transform.name);

            if (transform.gameObject.name.Equals("Left Controller")) localLeftHandGameObject = transform.gameObject;
            else if (transform.gameObject.name.Equals("Right Controller")) localRightHandGameObject = transform.gameObject;
            else if (transform.gameObject.name.Equals("Main Camera")) localHeadGameObject = transform.gameObject;
        }
    }

    //would use a generic method for these, but unity DOTS code gen no likey
    private RefRW<LocalTransform> GetEntityLeftHandTransform()
    {
        foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<NetworkedEntityChildComponent>().WithAll<LeftHandComponent>())
            return localTransform;

        foundLocalVRObjects = false;

        return default;
    }

    private RefRW<LocalTransform> GetEntityRightHandTransform()
    {
        foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<NetworkedEntityChildComponent>().WithAll<RightHandComponent>())
            return localTransform;

        foundLocalVRObjects = false;

        return default;
    }

    private RefRW<LocalTransform> GetEntityHeadTransform()
    {
        foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<NetworkedEntityChildComponent>().WithAll<HeadComponent>())
            return localTransform;

        foundLocalVRObjects = false;

        return default;
    }
}