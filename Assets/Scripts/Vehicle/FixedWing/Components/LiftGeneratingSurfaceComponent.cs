using Unity.Entities;

public partial struct LiftGeneratingSurfaceComponent : IComponentData, ComponentId
{
    public int Id { get; set; }

    public LowFidelityFixedAnimationCurve PitchAoALiftCoefficientPercentageCurve;

    public LowFidelityFixedAnimationCurve YawAoALiftCoefficientPercentageCurve;

    public Entity maxCenterOfLiftEntity;

    public Entity minCenterOfLiftEntity;
}