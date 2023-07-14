public enum NetworkMessageId : ushort
{
    ClientSyncOwnedEntities,
    ClientFinishedLoadingScene,
    ServerSyncEntities,
    ServerLoadScene,
    ServerSpawnEntity,
    ServerDestroyEntity
}