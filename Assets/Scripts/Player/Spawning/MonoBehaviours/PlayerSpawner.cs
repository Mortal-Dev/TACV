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
        Debug.Log("created");


        if (NetworkManager.Instance.NetworkType == NetworkType.Host)
        {
            ((HostNetwork)NetworkManager.Instance.Network).Server.ClientConnected += OnClientConnected;
        }
        else
        {
            ((ServerNetwork)NetworkManager.Instance.Network).Server.ClientConnected += OnClientConnected;
        }
    }

    protected override void OnDestroy()
    {
        Debug.Log("destroyed");
    }

    protected override void OnUpdate()
    {
        
    }

    private void OnClientConnected(object sender, ServerConnectedEventArgs serverConnectedEventArgs)
    {
        Debug.Log("client connected for spawn");

        NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.CreateNetworkedEntityFromIndex(0, serverConnectedEventArgs.Client.Id);
    }
}
