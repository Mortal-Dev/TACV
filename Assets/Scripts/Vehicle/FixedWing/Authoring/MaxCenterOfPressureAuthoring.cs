using Unity.Entities;
using UnityEngine;

public class MaxCenterOfPressureAuthoring : MonoBehaviour
{
    class Baking : Baker<MaxCenterOfPressureAuthoring>
    {
        public override void Bake(MaxCenterOfPressureAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new MaxCenterOfPressureComponent());
        }
    }
}