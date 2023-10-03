﻿public enum NetworkMessageId : ushort
{
    ClientSyncOwnedEntities,
    ClientFinishedLoadingScene,

    ClientRequestVehicleControl, //used if multiple clients can control a vehicle (like a 2 seater hornet), and swaps controls
    ServerConfirmClientRequestVehicleControl,
    ClientRequestVehicleEnter,
    ServerConfirmClientVehicleEnterRequest,
    ClientRequestVehicleExit,
    ServerConfirmClientVehicleLeaveRequest,

    ServerSetNetworkEntityParent,
    ServerUnparentNetworkEntity,

    ServerSyncEntity,
    ServerLoadScene,
    ServerSpawnEntity,
    ServerDestroyEntity,
    ServerDestroyDefaultSceneEntity,
    ServerChangeEntityOwnership
}