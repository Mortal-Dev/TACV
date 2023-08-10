using UnityEngine;
using Unity.Entities;
using System;

public class DragAuthoring : MonoBehaviour
{
    public float forwardProjectedArea;
    public DragCoefficientValue[] forwardDragAoACoefficients;

    public float backProjectedArea;
    public DragCoefficientValue[] backwardDragAoACoefficients;

    public float sideProjectedArea;
    public DragCoefficientValue[] sideDragAoACoefficients;

    class Baking : Baker<DragAuthoring>
    {
        public override void Bake(DragAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            FixedWingDragComponent fixedWingDragComponent = new FixedWingDragComponent();

            fixedWingDragComponent.forwardArea = authoring.forwardProjectedArea;
            fixedWingDragComponent.maxForwardDragCoefficient = authoring.forwardDragAoACoefficients[^1].dragCoefficient;
            fixedWingDragComponent.forwardDragCoefficientAoACurve = new LowFidelityFixedAnimationCurve();
            fixedWingDragComponent.forwardDragCoefficientAoACurve.Update(CreateCurveFromDragCoefficients(authoring.forwardDragAoACoefficients));

            fixedWingDragComponent.backArea = authoring.backProjectedArea;
            fixedWingDragComponent.maxBackDragCoefficient = authoring.backwardDragAoACoefficients[^1].dragCoefficient;
            fixedWingDragComponent.backDragCoefficientAoACurve = new LowFidelityFixedAnimationCurve();
            fixedWingDragComponent.backDragCoefficientAoACurve.Update(CreateCurveFromDragCoefficients(authoring.backwardDragAoACoefficients));

            fixedWingDragComponent.rightSideArea = authoring.sideProjectedArea;
            fixedWingDragComponent.maxRightSideDragCoefficient = authoring.sideDragAoACoefficients[^1].dragCoefficient;
            fixedWingDragComponent.rightSideDragCoefficientAoACurve = new LowFidelityFixedAnimationCurve();
            fixedWingDragComponent.rightSideDragCoefficientAoACurve.Update(CreateCurveFromDragCoefficients(authoring.sideDragAoACoefficients));

            fixedWingDragComponent.leftSideArea = authoring.sideProjectedArea;
            fixedWingDragComponent.maxLeftSideDragCoefficient = authoring.sideDragAoACoefficients[^1].dragCoefficient;
            fixedWingDragComponent.leftSideDragCoefficientAoACurve = new LowFidelityFixedAnimationCurve();
            fixedWingDragComponent.leftSideDragCoefficientAoACurve.Update(CreateCurveFromDragCoefficients(authoring.sideDragAoACoefficients));


            AddComponent(entity, fixedWingDragComponent);
        }

        private AnimationCurve CreateCurveFromDragCoefficients(DragCoefficientValue[] dragCoefficientInserts)
        {
            AnimationCurve animationCurve = new AnimationCurve();

            DragCoefficientValue maxDragCoefficient = dragCoefficientInserts[^1];

            foreach (DragCoefficientValue dragCoefficientValue in dragCoefficientInserts)
            {
                animationCurve.AddKey(dragCoefficientValue.angleOfAttack / maxDragCoefficient.angleOfAttack, dragCoefficientValue.dragCoefficient / maxDragCoefficient.dragCoefficient);
            }

            return animationCurve;
        }
    }
}

[Serializable]
public class DragCoefficientValue
{
    public float angleOfAttack;
    public float dragCoefficient;
}
