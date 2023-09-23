using Unity.Entities;
using UnityEngine;

public class VehicleAuthoring : MonoBehaviour
{
    class Baking : Baker<VehicleAuthoring>
    {
        public override void Bake(VehicleAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            VehicleComponent vehicleComponent = new VehicleComponent()
            {
                
            };

            AddComponent<VehicleComponent>(entity);
        }
    }
}