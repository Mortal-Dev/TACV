using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using Unity.XR.CoreUtils;
using Unity.Mathematics;

[ClientSystem]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class SyncLocalPlayerToXROriginSystem : SystemBase
{
    public GameObject XROriginGameObject;

    public GameObject localHeadGameObject;
    public GameObject localLeftHandGameObject;
    public GameObject localRightHandGameObject;

    public bool setXRTransformObjects = false;

    bool foundLocalVRObjects = true;

    protected override void OnUpdate()
    {
        if (!setXRTransformObjects) SetXRGameObjects();

        if (localHeadGameObject == null || localLeftHandGameObject == null || localRightHandGameObject == null) return;

        setXRTransformObjects = true;

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
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<PlayerComponent>())
            {
                XROriginGameObject.transform.SetPositionAndRotation(localTransform.ValueRO.Position, localTransform.ValueRO.Rotation);
            }
        else
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithAll<PlayerComponent>())
            {
                XROriginGameObject.transform.SetPositionAndRotation(localTransform.ValueRO.Position, localTransform.ValueRO.Rotation);
            }
    }

    private void SetXRGameObjects()
    {
        Debug.Log("setting VR objects");

        XROrigin xrOrigin = Object.FindFirstObjectByType<XROrigin>();

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
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LeftHandComponent>()) return localTransform;
        else
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithAll<LeftHandComponent>()) return localTransform;

        foundLocalVRObjects = false;

        return default;
    }

    private RefRW<LocalTransform> GetEntityRightHandTransform()
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<RightHandComponent>()) return localTransform;
        else
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithAll<RightHandComponent>()) return localTransform;

        foundLocalVRObjects = false;

        return default;
    }

    private RefRW<LocalTransform> GetEntityHeadTransform()
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.None)
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<HeadComponent>()) return localTransform;
        else
            foreach (RefRW<LocalTransform> localTransform in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<LocalOwnedNetworkedEntityComponent>().WithAll<HeadComponent>()) return localTransform;

        foundLocalVRObjects = false;

        return default;
    }
}