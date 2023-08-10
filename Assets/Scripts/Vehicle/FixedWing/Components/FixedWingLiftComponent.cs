using Unity.Entities;
using UnityEngine;

public partial struct FixedWingLiftComponent : IComponentData
{
    public float topArea;
    public float zeroAoALift;
    public float maxCoefficientLift;
    public float minCoefficientLift;
    public FixedAnimationCurve liftCurve;
}