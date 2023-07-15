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

            FixedList128Bytes<short> childEntityMap = CalulateEntityChildMap(authoring.gameObject);

            AddComponent(entity, new NetworkedEntityChildComponent() { Id =  random.Next(int.MinValue, int.MaxValue) } );
            AddComponent(entity, new PreviousLocalTransformRecordComponent() { localTransformRecord = new LocalTransform() { Position = authoring.transform.localPosition, Rotation = authoring.transform.localRotation, Scale = authoring.transform.localScale.y } });
        }

        private FixedList128Bytes<short> CalulateEntityChildMap(GameObject childGameObject)
        {
            Transform root = childGameObject.transform.root;

            Transform traversalTransform = childGameObject.transform;

            List<short> childMap = new List<short>();

            while (traversalTransform != root)
            {
                childMap.Add(FindGameObjectIndex(traversalTransform, traversalTransform.parent.GetComponentsInChildren<Transform>()));

                traversalTransform = traversalTransform.parent;
            }

            //reverse since we will be using it from parent to child instead of child to parent later
            childMap.Reverse();

            return FillFixedList(childMap.ToArray());
        }

        private short FindGameObjectIndex(Transform gameObject, Transform[] siblings)
        {
            for (int i = 0; i < siblings.Length; i++)
            {
                if (siblings[i] == gameObject) return (short)i;
            }

            throw new System.Exception("unable to find gameobject");
        }

        private FixedList128Bytes<short> FillFixedList(short[] shorts)
        {
            FixedList128Bytes<short> fixedListShorts = new FixedList128Bytes<short>();

            foreach (short value in shorts)
            {
                fixedListShorts.Add(value);
            }

            return fixedListShorts;
        }
    }
}