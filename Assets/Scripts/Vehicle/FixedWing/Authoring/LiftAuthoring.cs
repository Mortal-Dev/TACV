using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public partial class LiftAuthoring : MonoBehaviour
{
    public float fixedWingTopArea;

    public float zeroAoALift;

    public List<LiftCoefficientValue> liftCoefficientAoAValues;

    class Baking : Baker<LiftAuthoring>
    {
        public override void Bake(LiftAuthoring authoring)
        {
            FixedWingLiftComponent fixedWingLiftComponent = new FixedWingLiftComponent();

            float largestLiftCoefficient = FindLargestLiftCoefficient(authoring.liftCoefficientAoAValues);

            fixedWingLiftComponent.zeroAoALift = authoring.zeroAoALift;
            fixedWingLiftComponent.topArea = authoring.fixedWingTopArea;
            fixedWingLiftComponent.maxCoefficientLift = largestLiftCoefficient;
            fixedWingLiftComponent.liftCurve = new FixedAnimationCurve();
            fixedWingLiftComponent.liftCurve.Update(CreateCurveFromLiftCoefficients(authoring.liftCoefficientAoAValues, largestLiftCoefficient));

            AddComponent(GetEntity(TransformUsageFlags.Dynamic), fixedWingLiftComponent);
        }

        private AnimationCurve CreateCurveFromLiftCoefficients(List<LiftCoefficientValue> liftCoefficients, float largestLiftCoefficient)
        {
            AnimationCurve animationCurve = new AnimationCurve();

            foreach (LiftCoefficientValue liftCoefficientValue in liftCoefficients)
            {
                animationCurve.AddKey((liftCoefficientValue.angleOfAttack + 90f) / 180f, Mathf.Abs(liftCoefficientValue.liftCoefficient) / largestLiftCoefficient);
            }

            return animationCurve;
        }

        private float FindLargestLiftCoefficient(List<LiftCoefficientValue> liftCoefficients)
        {
            float largestLiftCoefficient = 0f;

            foreach (LiftCoefficientValue liftCoefficientValue in liftCoefficients)
            {
                if (liftCoefficientValue.angleOfAttack <= 0) continue;

                if (liftCoefficientValue.liftCoefficient < largestLiftCoefficient) return largestLiftCoefficient;
                
                largestLiftCoefficient = liftCoefficientValue.liftCoefficient;
            }

            return largestLiftCoefficient;
        }
    }
}

[Serializable]
public class LiftCoefficientValue
{
    public float angleOfAttack;
    public float liftCoefficient;
}
