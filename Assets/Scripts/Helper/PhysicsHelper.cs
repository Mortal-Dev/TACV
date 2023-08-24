using Unity.Entities;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;

public static class PhysicsHelper
{
    public static NativeList<ColliderCastHit> SphereCast(CollisionFilter collisionFilter, float3 rayFrom, float3 direction, float radius, float length, EntityManager entityManager)
    {
        EntityQueryBuilder builder = new EntityQueryBuilder(Allocator.Temp).WithAll<PhysicsWorldSingleton>();

        EntityQuery singletonQuery = entityManager.CreateEntityQuery(builder);
        CollisionWorld collisionWorld = singletonQuery.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        singletonQuery.Dispose();

        NativeList<ColliderCastHit> colliderHits = new NativeList<ColliderCastHit>(Allocator.Temp);

        collisionWorld.SphereCastAll(rayFrom, radius, direction, length, ref colliderHits, collisionFilter);

        return colliderHits;
    }
}
