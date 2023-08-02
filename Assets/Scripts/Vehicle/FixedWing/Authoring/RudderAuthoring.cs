using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class RudderAuthoring : MonoBehaviour
{
    public float maxRudderAngleDegrees;

    public float maxRudderDrag;

    class Baking : Baker<RudderAuthoring>
    {
        public override void Bake(RudderAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new RudderComponent() { maxRudderAngleDegrees = authoring.maxRudderAngleDegrees, maxRudderDrag = authoring.maxRudderDrag });
        }
    }
}