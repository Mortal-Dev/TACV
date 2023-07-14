using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Riptide;
using Unity.Entities;

[DisableAutoCreation]
[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[ServerSystem]
public partial class PlayerSpawner : SystemBase
{
    protected override void OnCreate()
    {
        if (NetworkManager.Instance.NetworkType == NetworkType.Host)
        {
            ((HostNetwork)NetworkManager.Instance.Network).Server.ClientConnected += OnClientConnected;
        }
        else
        {
            ((ServerNetwork)NetworkManager.Instance.Network).Server.ClientConnected += OnClientConnected;
        }
    }

    protected override void OnUpdate()
    {
        
    }

    private void OnClientConnected(object sender, ServerConnectedEventArgs serverConnectedEventArgs)
    {
        NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.CreateNetworkedEntityFromIndex(0, serverConnectedEventArgs.Client.Id);
    }
}
