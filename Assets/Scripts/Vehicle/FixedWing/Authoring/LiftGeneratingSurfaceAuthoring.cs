using UnityEngine;
using Unity.Entities;
using System;

public class LiftGeneratingSurfaceAuthoring : MonoBehaviour
{
    public int positionId;

    public AoALiftCoefficientPercentageValue[] AoALiftCoefficientPercentageValues;

    class Baking : Baker<LiftGeneratingSurfaceAuthoring>
    {
        public override void Bake(LiftGeneratingSurfaceAuthoring authoring)
        {
            LowFidelityFixedAnimationCurve lowFidelityFixedAnimationCurve = new LowFidelityFixedAnimationCurve();

            lowFidelityFixedAnimationCurve.SetCurve(CreateAnimationCurveFromAoALiftCoefficientValues(authoring.AoALiftCoefficientPercentageValues));

            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new LiftGeneratingSurfaceComponent() { Id = authoring.positionId, AoALiftCoefficientPercentageCurve = lowFidelityFixedAnimationCurve });
        }

        private AnimationCurve CreateAnimationCurveFromAoALiftCoefficientValues(AoALiftCoefficientPercentageValue[] aoALiftCoefficientPercentageValues)
        {
            AnimationCurve animationCurve = new AnimationCurve();

            foreach (AoALiftCoefficientPercentageValue value in aoALiftCoefficientPercentageValues)
            {
                animationCurve.AddKey(value.AngleOfAttack + 90f / 180f, value.LiftCoefficientPercentage);
            }

            return animationCurve;
        }
    }

    [Serializable]
    public class AoALiftCoefficientPercentageValue
    {
        [Range(-90, 90)] public float AngleOfAttack;
        [Range(0f, 1f)] public float LiftCoefficientPercentage;
    }
}