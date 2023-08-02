using UnityEngine;
using Unity.Entities;

public class FixedWingDragAuthoring : MonoBehaviour
{
    public float maxForwardDrag;
    public AnimationCurve forwardDrag;

    public float maxBackDrag;
    public AnimationCurve backDrag;

    public float maxTopDrag;
    public AnimationCurve topDrag;

    public float maxBottomDrag;
    public AnimationCurve bottomDrag;

    public float maxLeftSideDrag;
    public AnimationCurve leftSideDrag;

    public float maxRightSideDrag;
    public AnimationCurve rightSideDrag;

    class Baking : Baker<FixedWingDragAuthoring>
    {
        public override void Bake(FixedWingDragAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            FixedWingDragComponent fixedWingDragComponent = new FixedWingDragComponent();

            fixedWingDragComponent.maxForwardDrag = authoring.maxForwardDrag;
            fixedWingDragComponent.forwardDrag = new FixedAnimationCurve();
            fixedWingDragComponent.forwardDrag.Update(authoring.forwardDrag);

            fixedWingDragComponent.maxBackDrag = authoring.maxBackDrag;
            fixedWingDragComponent.backDrag = new FixedAnimationCurve();
            fixedWingDragComponent.backDrag.Update(authoring.backDrag);

            fixedWingDragComponent.maxTopDrag = authoring.maxTopDrag;
            fixedWingDragComponent.topDrag = new FixedAnimationCurve();
            fixedWingDragComponent.topDrag.Update(authoring.topDrag);

            fixedWingDragComponent.maxBottomDrag = authoring.maxBottomDrag;
            fixedWingDragComponent.bottomDrag = new FixedAnimationCurve();
            fixedWingDragComponent.bottomDrag.Update(authoring.bottomDrag);

            fixedWingDragComponent.maxRightSideDrag = authoring.maxRightSideDrag;
            fixedWingDragComponent.rightSideDrag = new FixedAnimationCurve();
            fixedWingDragComponent.rightSideDrag.Update(authoring.rightSideDrag);

            fixedWingDragComponent.maxLeftSideDrag = authoring.maxLeftSideDrag;
            fixedWingDragComponent.leftSideDrag = new FixedAnimationCurve();
            fixedWingDragComponent.leftSideDrag.Update(authoring.leftSideDrag);

            AddComponent(entity, fixedWingDragComponent);
        }
    }
}
