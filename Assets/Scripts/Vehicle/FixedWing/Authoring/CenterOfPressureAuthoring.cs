using UnityEngine;
using Unity.Entities;

public class CenterOfPressureAuthoring : MonoBehaviour
{
    class Baking : Baker<CenterOfPressureAuthoring>
    {
        public override void Bake(CenterOfPressureAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new CenterOfPressureComponent());
        }
    }
}