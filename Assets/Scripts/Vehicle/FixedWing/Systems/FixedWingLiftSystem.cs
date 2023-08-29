using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using System.Runtime.InteropServices;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(FixedWingStateSystem))]
public partial struct FixedWingLiftSystem : ISystem
{
    EntityQuery networkEntityQuery;

    ComponentLookup<FixedWingLiftComponent> fixedWingLiftComponentLookup;

    public void OnCreate(ref SystemState systemState)
    {
        networkEntityQuery = systemState.GetEntityQuery(ComponentType.ReadWrite<LiftGeneratingSurfaceComponent>(), ComponentType.ReadWrite<LocalTransform>(), ComponentType.ReadOnly<Parent>(),
            ComponentType.ReadOnly<NetworkedEntityChildComponent>());

        fixedWingLiftComponentLookup = systemState.GetComponentLookup<FixedWingLiftComponent>();

    }

    public void OnUpdate(ref SystemState systemState)
    {
        if (!SystemAPI.TryGetSingleton(out NetworkManagerEntityComponent networkManagerEntityComponent)) return;

        fixedWingLiftComponentLookup.Update(ref systemState);

        if (networkManagerEntityComponent.NetworkType == NetworkType.None)
        {
            new UpdateLiftJob() { fixedWingLiftComponentLookup = fixedWingLiftComponentLookup, deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(systemState.Dependency).Complete();
        }
        else
        {
            new UpdateLiftJob() { fixedWingLiftComponentLookup = fixedWingLiftComponentLookup, deltaTime = SystemAPI.Time.DeltaTime }.ScheduleParallel(networkEntityQuery, systemState.Dependency).Complete();
        }

    }

    [StructLayout(LayoutKind.Auto)]
    partial struct UpdateLiftJob : IJobEntity
    {
        [ReadOnly] public float deltaTime;

//        public EntityCommandBuffer.ParallelWriter parallelWriter;

       // public EntityManager entityManager;

        [ReadOnly] public ComponentLookup<FixedWingLiftComponent> fixedWingLiftComponentLookup;

        public void Execute(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform localTransform, in Parent parent)
        {
            if (!fixedWingLiftComponentLookup.TryGetComponent(parent.Value, out FixedWingLiftComponent fixedWingLiftComponent))
            {
                Debug.LogError("attempting to put lift on a non fixed wing, make sure all lift gameobject/entities are direct children of the fixed wing");
                return;
            }

            ProcessPitchLift(ref liftGeneratingSurfaceComponent, ref localTransform, fixedWingLiftComponent, in parent);
        }

        private void ProcessPitchLift(ref LiftGeneratingSurfaceComponent liftGeneratingSurfaceComponent, ref LocalTransform liftGeneratingSurfaceLocalTransform, 
            FixedWingLiftComponent fixedWingLiftComponent, in Parent parent)
        {
            float3 liftGeneratingSurfaceGlobalPosition = float3.zero;//liftGeneratingSurfaceLocalTransform.TransformPoint(liftGeneratingSurfaceLocalTransform.Position);

            float difference = Vector3.Distance(liftGeneratingSurfaceGlobalPosition, new float3(0, 0, 0));

            float speed = difference / deltaTime;

            float pitchAngleOfAttack = 0;

            float yawAngleOfAttack;

            float pitchLiftCoefficient = fixedWingLiftComponent.pitchLiftCurve.Evaluate(pitchAngleOfAttack) * liftGeneratingSurfaceComponent.PitchAoALiftCoefficientPercentageCurve.Evaluate(pitchAngleOfAttack);

            //float pitchLiftForce = pitchLiftCoefficient * (AirDensity.GetAirDensityFromMeters(liftGeneratingSurfaceGlobalPosition.y) * 0.5f) * (speed * speed) * fixedWingLiftComponent.topArea;

            //PhysicsVelocity physicsVelocity = entityManager.GetComponentData<PhysicsVelocity>(parent.Value);

           // PhysicsMass physicsMass = entityManager.GetComponentData<PhysicsMass>(parent.Value);

            Quaternion test = quaternion.AxisAngle(liftGeneratingSurfaceLocalTransform.Up(), 90);

            Debug.Log(test.eulerAngles);

            //physicsVelocity.ApplyImpulse(physicsMass, physicsMass.Transform.pos, physicsMass.Transform.rot, quaternion.AxisAngle(liftGeneratingSurfaceLocalTransform.Up(), 90) * 
        }
    }

    /*private void UpdateLift(RefRO<FixedWingComponent> fixedWingComponent, RefRW<FixedWingLiftComponent> fixedWingLiftComponent, RefRO<PhysicsMass> physicsMass, RefRO<LocalTransform> localTransform, 
        RefRW<PhysicsVelocity> physicsVelocity, ref SystemState systemState)
    {
        float angleOfAttack = fixedWingComponent.ValueRO.angleOfAttack;

        float angleOfAttackPercent = (angleOfAttack + 90f) / 180f;

        Vector3 forward = localTransform.ValueRO.Forward();

        float liftCoefficientPercent = fixedWingLiftComponent.ValueRO.liftCurve.Evaluate(angleOfAttackPercent);

        float liftCoefficient = (fixedWingLiftComponent.ValueRO.maxCoefficientLift - fixedWingLiftComponent.ValueRO.minCoefficientLift) * liftCoefficientPercent + fixedWingLiftComponent.ValueRO.minCoefficientLift;

        float liftPower = liftCoefficient * (AirDensity.GetAirDensityFromMeters(localTransform.ValueRO.Position.y) * 0.5f)
            * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * fixedWingLiftComponent.ValueRO.topArea;

        float3 centerOfPressure = SystemAPI.GetComponent<LocalTransform>(fixedWingComponent.ValueRO.centerOfPressureEntity).Position;

        //physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, RotateVectorByX(vec3.normalized, 90f) * liftPower * SystemAPI.Time.DeltaTime);

        Debug.Log("up vector: " + localTransform.ValueRO.Up());
        Debug.Log("lift vector: " + RotateVectorByX(forward, 90f));

     //   physicsVelocity.ValueRW.ApplyLinearImpulse(physicsMass.ValueRO, localTransform.ValueRO.Up() * liftPower * SystemAPI.Time.DeltaTime);
       // physicsVelocity.ValueRW.ApplyImpulse(physicsMass.ValueRO, physicsMass.ValueRO.Transform.pos, physicsMass.ValueRO.Transform.rot, localTransform.ValueRO.Up() * liftPower * SystemAPI.Time.DeltaTime, centerOfPressure);

        Debug.Log("altitude ft: " + localTransform.ValueRO.Position.y * 3.28084f);
        Debug.Log("speed kts: " + ((Vector3)physicsVelocity.ValueRO.Linear).magnitude * 1.943844f);
    }*/

    // Function to rotate a Vector3 by a specified angle around the X-axis
    private float3 RotateVectorByX(float3 vector, float angle)
    {
        angle *= -1f;

        float radianAngle = angle * Mathf.Deg2Rad;
        float sinAngle = math.sin(radianAngle);
        float cosAngle = math.cos(radianAngle);

        float3 rotatedVector = new float3(
            vector.x,
            vector.y * cosAngle - vector.z * sinAngle,
            vector.y * sinAngle + vector.z * cosAngle
        );

        return rotatedVector;
    }
}