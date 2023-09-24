using Unity.Burst;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using System.Diagnostics;

[BurstCompile]
[UpdateInGroup(typeof(PresentationSystemGroup), OrderFirst = true)]
public partial struct FakeChildSystem : ISystem
{
    ComponentLookup<LocalTransform> localTransformLookup;

    ComponentLookup<Parent> parentLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState systemState)
    {
        localTransformLookup = systemState.GetComponentLookup<LocalTransform>();
        parentLookup = systemState.GetComponentLookup<Parent>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState systemState)
    {
        localTransformLookup.Update(ref systemState);

        new FakeChildJob() { localTransformLookup = localTransformLookup, parentLookup = parentLookup }.ScheduleParallel(systemState.Dependency).Complete();
    }

    [BurstCompile]
    partial struct FakeChildJob : IJobEntity
    {
        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public ComponentLookup<LocalTransform> localTransformLookup;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly]
        public ComponentLookup<Parent> parentLookup;

        public void Execute(ref LocalTransform localTransform, in FakeChildComponent fakeChildComponent)
        {
            LocalTransform parentTransform = localTransformLookup[fakeChildComponent.parent];

            Parent fakeChildParentParent = parentLookup[fakeChildComponent.parent];

            LocalTransform fakeChildParentParentLocalTransform = localTransformLookup[fakeChildParentParent.Value];

            LocalTransform newLocalTransform = parentTransform.TransformTransform(fakeChildParentParentLocalTransform);

            localTransform.Position = newLocalTransform.Position;
            localTransform.Rotation = newLocalTransform.Rotation;
            localTransform.Scale = newLocalTransform.Scale;
        }
    }
}
