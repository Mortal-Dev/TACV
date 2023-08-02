using Unity.Entities;
using UnityEngine;

public partial struct FixedWingDragComponent : IComponentData
{
    public float maxForwardDrag;
    public FixedAnimationCurve forwardDrag;

    public float maxBackDrag;
    public FixedAnimationCurve backDrag;

    public float maxTopDrag;
    public FixedAnimationCurve topDrag;

    public float maxBottomDrag;
    public FixedAnimationCurve bottomDrag;

    public float maxLeftSideDrag;
    public FixedAnimationCurve leftSideDrag;

    public float maxRightSideDrag;
    public FixedAnimationCurve rightSideDrag;
}