using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;

public partial struct FixedWingComponent : IComponentData
{
    public float3 gForce;

    public float3 lastVelocity;

    public float3 localVelocity;

    public float3 localAngularVelocity;

    public FixedList128Bytes<RefRW<EngineComponent>> engineComponents;

    public FixedList128Bytes<RefRW<RudderComponent>> rudderComponents;

    public FixedList128Bytes<RefRW<FlapComponent>> flapComponents;

    public FixedList128Bytes<RefRW<StabilatorComponent>> stabilatorComponents;

    public FixedList128Bytes<RefRW<AirleronComponent>> airleronComponents;

    public float throttle;
}