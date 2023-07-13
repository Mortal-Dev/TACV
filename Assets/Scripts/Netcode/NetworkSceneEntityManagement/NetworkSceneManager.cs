using Riptide;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkSceneManager
{
    public World NetworkWorld { get; private set; }

    public NetworkedObjectContainer NetworkedObjectContainer { get; private set; }

    public float TicksPerSecond 
    {   
        get
        {
            return ((FixedStepSimulationSystemGroup)NetworkWorld.GetExistingSystemManaged(typeof(FixedStepSimulationSystemGroup))).Timestep;
        }
    }

    private string sceneToLoadName;
    
    public NetworkSceneManager(int ticksPerSecond)
    {
        switch (NetworkManager.Instance.NetworkType)
        {
            case NetworkType.Client:
                NetworkWorld = CreateNetworkWorld<ClientSystemAttribute>("ClientWorld", ticksPerSecond);
                NetworkedObjectContainer = new ClientNetworkedObjectContainer(NetworkWorld.EntityManager);
                break;
            case NetworkType.Server:
                NetworkWorld = CreateNetworkWorld<ServerSystemAttribute>("ServerWorld", ticksPerSecond);
                NetworkedObjectContainer = new ServerNetworkedObjectContainer(NetworkWorld.EntityManager);

                ((ServerNetwork)NetworkManager.Instance.Network).Server.ClientConnected += (o, e) =>
                {
                    SendServerLoadSceneMessage(sceneToLoadName, e.Client.Id);
                };
                break;
            case NetworkType.Host:
                NetworkWorld = CreateNetworkWorld<NetworkSystemBaseAttribute>("HostWorld", ticksPerSecond);
                NetworkedObjectContainer = new HostNetworkedObjectContainer(NetworkWorld.EntityManager);

                ((HostNetwork)NetworkManager.Instance.Network).Server.ClientConnected += (o, e) =>
                {
                    SendServerLoadSceneMessage(sceneToLoadName, e.Client.Id);
                };
                break;
        }

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(NetworkWorld, GetNonDisableAutoCreateSystem());

        World.DefaultGameObjectInjectionWorld = NetworkWorld;
    }

    public AsyncOperation LoadScene(string sceneName)
    {
        AsyncOperation newSceneOperation;

        sceneToLoadName = sceneName;

        if (NetworkManager.Instance.NetworkType == NetworkType.Host || NetworkManager.Instance.NetworkType == NetworkType.Server)
        {
            newSceneOperation = SceneManager.LoadSceneAsync(sceneName);

            newSceneOperation.completed += (AsyncOperation ao) =>
            {
                NetworkedObjectContainer.DestroyAllNetworkedEntities();

                if (NetworkManager.Instance.NetworkType == NetworkType.Host)
                {
                    NetworkWorld = CreateNetworkWorld<NetworkSystemBaseAttribute>("HostWorld", TicksPerSecond);
                    NetworkedObjectContainer = new HostNetworkedObjectContainer(NetworkWorld.EntityManager);
                }
                else
                {
                    NetworkWorld = CreateNetworkWorld<ServerSystemAttribute>("ServerWorld", TicksPerSecond);
                    NetworkedObjectContainer = new ServerNetworkedObjectContainer(NetworkWorld.EntityManager);
                }
            };

            SendServerLoadSceneMessage(sceneName, NetworkManager.SERVER_NET_ID);
        }
        else
        {
            newSceneOperation = SceneManager.LoadSceneAsync(sceneName);

            newSceneOperation.completed += (AsyncOperation asyncOperation) =>
            {
                NetworkedObjectContainer.DestroyAllNetworkedEntities();
                SendClientCompletedSceneMessage();
            };
        }

        return newSceneOperation;
    }

    public AsyncOperation LoadScene(int sceneIndex) => LoadScene(SceneManager.GetSceneByBuildIndex(sceneIndex).name);

    private World CreateNetworkWorld<TNetworkSystemAttribute>(string worldName, float tickPerSecond) where TNetworkSystemAttribute : Attribute
    {
        World world = new World(worldName);

        //clone entity prefab manager into networked world
        world.EntityManager.Instantiate(World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkedPrefabsComponent>()).GetSingletonEntity());

        Type[] systemTypes = AttributeUsageFinder.GetUsages<TNetworkSystemAttribute>();

        foreach (Type type in systemTypes) world.CreateSystem(type);

        //shadow wizard casting gang (we love casting objects)
        ((FixedStepSimulationSystemGroup)world.GetExistingSystemManaged(typeof(FixedStepSimulationSystemGroup))).Timestep = 1f / tickPerSecond;

        return world;
    }

    private List<Type> GetNonDisableAutoCreateSystem()
    {
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.All);

        List<Type> nonDisableAutoCreateSystems = new List<Type>();

        foreach (Type system in systems)
        {
            bool foundDisableAutoCreate = false;

            foreach (var attribute in system.GetCustomAttributes(true)) if (attribute.GetType().Equals(typeof(DisableAutoCreationAttribute))) foundDisableAutoCreate = true;

            if (!foundDisableAutoCreate) nonDisableAutoCreateSystems.Add(system);
        }

        return nonDisableAutoCreateSystems;
    }

    private void SendClientCompletedSceneMessage()
    {
        Message message = Message.Create(MessageSendMode.Reliable, (ushort)NetworkMessageId.ClientFinishedLoadingScene);

        message.Add(sceneToLoadName);

        NetworkManager.Instance.Network.SendMessage(message, SendMode.Client);
    }

    private void SendSpawnNetworkedEntityMessage(int prefabHash, ushort connectionOwnerId, LocalTransform localTransform, ulong networkedEntityId, ushort sendToClientId = NetworkManager.SERVER_NET_ID)
    {
        Message message = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerSpawnEntity);

        message.Add(prefabHash);
        message.Add(connectionOwnerId);
        message.Add(networkedEntityId);
        message.AddLocalTransform(localTransform);

        NetworkManager.Instance.Network.SendMessage(message, SendMode.Server, sendToClientId);
    }

    private void SendServerLoadSceneMessage(string sceneName, ushort sendToClientId)
    {
        Message message = Message.Create(MessageSendMode.Reliable, (ushort)NetworkMessageId.ServerLoadScene);

        message.AddString(sceneName);

        NetworkManager.Instance.Network.SendMessage(message, SendMode.Server, sendToClientId);
    }

    [MessageHandler((ushort)NetworkMessageId.ClientFinishedLoadingScene)]
    private static void ClientFinishedLoadingScene(ushort clientId, Message message)
    {
        if (clientId == NetworkManager.CLIENT_NET_ID) return;

        string sceneClientFinishedLoadingName = message.GetString();

        if (!sceneClientFinishedLoadingName.Equals(NetworkManager.Instance.NetworkSceneManager.sceneToLoadName))
        {
            Debug.LogWarning($"client sending completed load scene: {sceneClientFinishedLoadingName}, which is not the current scene: {SceneManager.GetActiveScene().name}");
            return;
        }

        IEnumerator<KeyValuePair<ulong, Entity>> enumerator = NetworkManager.Instance.NetworkSceneManager.NetworkedObjectContainer.GetEntities();

        while (enumerator.MoveNext())
        {
            KeyValuePair<ulong, Entity> idEntityPair = enumerator.Current;

            NetworkedEntityComponent networkedEntityComponent = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(idEntityPair.Value);

            LocalTransform localTransform = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<LocalTransform>(idEntityPair.Value);

            NetworkManager.Instance.NetworkSceneManager.SendSpawnNetworkedEntityMessage(networkedEntityComponent.networkedPrefabHash, networkedEntityComponent.connectionId, localTransform, 
                networkedEntityComponent.networkEntityId, clientId);
        }
    }

    [MessageHandler((ushort)NetworkMessageId.ServerLoadScene)]
    private static void ServerLoadScene(Message message)
    {
        NetworkManager.Instance.NetworkSceneManager.LoadScene(message.GetString());
    }
}