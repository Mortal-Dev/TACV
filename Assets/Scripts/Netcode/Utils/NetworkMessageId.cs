public enum NetworkMessageId : ushort
{
    ClientSyncOwnedEntities,
    ClientFinishedLoadingScene,
    ClientRequestVehicleOwnership,
    ServerSyncEntity,
    ServerLoadScene,
    ServerSpawnEntity,
    ServerDestroyEntity,
    ServerDestroyDefaultSceneEntity
}