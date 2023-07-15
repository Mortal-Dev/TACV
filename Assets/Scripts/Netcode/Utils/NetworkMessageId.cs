public enum NetworkMessageId : ushort
{
    ClientSyncOwnedEntities,
    ClientFinishedLoadingScene,
    ServerSyncEntity,
    ServerLoadScene,
    ServerSpawnEntity,
    ServerDestroyEntity,
    ServerDestroyDefaultSceneEntity
}