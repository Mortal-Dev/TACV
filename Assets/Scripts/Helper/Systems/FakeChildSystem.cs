using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial struct FakeChildSystem : ISystem
{
    ComponentLookup<LocalTransform> localTransformLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        localTransformLookup = systemState.GetComponentLookup<LocalTransform>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        localTransformLookup.Update(ref systemState);

        new FakeChildJob() { localTransformLookup = localTransformLookup }.ScheduleParallel(systemState.Dependency).Complete();
    }

    [BurstCompile]
    partial struct FakeChildJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;

        public void Execute(ref LocalTransform localTransform, in FakeChildComponent fakeChildComponent)
        {
            LocalTransform parentTransform = localTransformLookup[fakeChildComponent.parent];

            LocalTransform newLocalTransform = parentTransform.TransformTransform(localTransform); 

            localTransform.Position = newLocalTransform.Position;
            localTransform.Rotation = newLocalTransform.Rotation;
            localTransform.Scale = newLocalTransform.Scale;
        }
    }
}
