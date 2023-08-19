using Unity.Entities;
using Unity.Mathematics;

public partial struct CenterOfPressureComponent : IComponentData
{
    public float3 maxCenterOfPressure;
    public float3 minCenterOfPressure;

    public LowFidelityFixedAnimationCurve centerOfPressureCurve;
}