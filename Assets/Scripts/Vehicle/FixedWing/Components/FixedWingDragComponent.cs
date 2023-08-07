using Unity.Entities;
using UnityEngine;

public partial struct FixedWingDragComponent : IComponentData
{
    public float maxForwardDragCoefficient;
    public float forwardArea;
    public FixedAnimationCurve forwardDragCoefficientAoACurve;

    public float maxBackDragCoefficient;
    public float backArea;
    public FixedAnimationCurve backDragCoefficientAoACurve;


    public float maxLeftSideDragCoefficient;
    public float leftSideArea;
    public FixedAnimationCurve leftSideDragCoefficientAoACurve;

    public float maxRightSideDragCoefficient;
    public float rightSideArea;
    public FixedAnimationCurve rightSideDragCoefficientAoACurve;
}