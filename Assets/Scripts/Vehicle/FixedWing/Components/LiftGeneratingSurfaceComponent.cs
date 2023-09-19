using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public partial struct LiftGeneratingSurfaceComponent : IComponentData, ComponentId
{
    public int Id { get; set; }

    public float liftArea;

    public LowFidelityFixedAnimationCurve PitchAoALiftCoefficientPercentageCurve;

    public LowFidelityFixedAnimationCurve YawAoALiftCoefficientPercentageCurve;

    public Entity liftEntity;

    public Entity maxCenterOfLiftEntity;

    public Entity minCenterOfLiftEntity;

    public LocalTransform lastGlobalTransform;

    public float3 lastLocalPosition;
}