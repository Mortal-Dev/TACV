using System.Collections.Generic;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using UnityEngine;

public class NetworkedEntityChildAuthoring : MonoBehaviour
{
    class Baking : Baker<NetworkedEntityChildAuthoring>
    {
        System.Random random = new System.Random();

        public override void Bake(NetworkedEntityChildAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            NetworkedEntityChildComponent component = new NetworkedEntityChildComponent() { Id = random.Next(int.MinValue, int.MaxValue) };

            //impossible for id to be set to zero again, so we re roll if it is set, 0 is an id later used to identity the parent entity in the network sync message
            if (component.Id == 0) component.Id = random.Next(int.MinValue, int.MaxValue);

            AddComponent(entity, new NetworkedEntityChildComponent() { Id =  random.Next(int.MinValue, int.MaxValue) } );
            AddComponent(entity, new PreviousLocalTransformRecordComponent() { localTransformRecord = new LocalTransform() { Position = authoring.transform.localPosition, Rotation = authoring.transform.localRotation, Scale = authoring.transform.localScale.y } });
        }
    }
}