using Unity.Entities;
using Unity.Mathematics;

public partial struct LiftGeneratingSurfaceComponent : IComponentData, ComponentId
{
    public int Id { get; set; }

    public LowFidelityFixedAnimationCurve PitchAoALiftCoefficientPercentageCurve;

    public LowFidelityFixedAnimationCurve YawAoALiftCoefficientPercentageCurve;

    public Entity liftEntity;

    public Entity maxCenterOfLiftEntity;

    public Entity minCenterOfLiftEntity;

    public float3 lastGlobalPosition;

    public float3 lastLocalPosition;

    public float3 calculatedLiftForce;
}