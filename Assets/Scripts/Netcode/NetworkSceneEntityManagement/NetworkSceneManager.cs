using Riptide;
using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Collections;

public class NetworkSceneManager
{
    public World NetworkWorld { get; private set; }

    public NetworkedEntityContainer NetworkedEntityContainer { get; private set; }

    private string sceneToLoadName;

    AsyncOperation ao;

    public AsyncOperation LoadScene(string sceneName)
    {
        Debug.Log(World.DefaultGameObjectInjectionWorld == null);

        Debug.Log("called load scene");

        AsyncOperation newSceneOperation;

        sceneToLoadName = sceneName;

        SceneFinishedLoadingSystem sceneFinishedLoadingSystem = (SceneFinishedLoadingSystem)World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged(typeof(SceneFinishedLoadingSystem));

        if (NetworkManager.Instance.NetworkType == NetworkType.Host || NetworkManager.Instance.NetworkType == NetworkType.Server)
        {
            newSceneOperation = SceneManager.LoadSceneAsync(sceneName);

            Debug.Log("beginning loading scene");

            newSceneOperation.completed += (AsyncOperation ao) =>
            {
                sceneFinishedLoadingSystem.StartChecking();

                sceneFinishedLoadingSystem.FinishedLoadingSubScenesCompleted += () =>
                {
                    NetworkedEntityContainer?.DestroyAllNetworkedEntities();

                    if (NetworkManager.Instance.NetworkType == NetworkType.Host)
                    {
                        NetworkWorld = CreateNetworkWorld<NetworkSystemBaseAttribute>("HostWorld", NetworkManager.TICKS_PER_SECOND);
                        NetworkedEntityContainer = new HostNetworkedEntityContainer(NetworkWorld.EntityManager);
                        NetworkedEntityContainer.SetupSceneNetworkedEntities();
                    }
                    else
                    {
                        NetworkWorld = CreateNetworkWorld<ServerSystemAttribute>("ServerWorld", NetworkManager.TICKS_PER_SECOND);
                        NetworkedEntityContainer = new ServerNetworkedEntityContainer(NetworkWorld.EntityManager);
                        NetworkedEntityContainer.SetupSceneNetworkedEntities();
                    }

                    sceneFinishedLoadingSystem.StopChecking();
                };
            };

            SendServerLoadSceneMessage(sceneName, NetworkManager.SERVER_NET_ID);
        }
        else
        {
            newSceneOperation = SceneManager.LoadSceneAsync(sceneName);

            Debug.Log("beginning loading scene");

            newSceneOperation.completed += (AsyncOperation asyncOperation) =>
            {
                sceneFinishedLoadingSystem.StartChecking();

                sceneFinishedLoadingSystem.FinishedLoadingSubScenesCompleted += () =>
                {
                    NetworkedEntityContainer?.DestroyAllNetworkedEntities();

                    NetworkWorld = CreateNetworkWorld<ClientSystemAttribute>("ClientWorld", NetworkManager.TICKS_PER_SECOND);

                    NetworkedEntityContainer networkedEntityContainer = new ClientNetworkedEntityContainer(NetworkWorld.EntityManager);

                    NetworkedEntityContainer.SetupSceneNetworkedEntities();
                    SendClientCompletedSceneMessage();

                    sceneFinishedLoadingSystem.StopChecking();
                };
            };
        }

        SetupConnectionEvents();

        return newSceneOperation;
    }

    public AsyncOperation LoadScene(int sceneIndex) => LoadScene(SceneManager.GetSceneByBuildIndex(sceneIndex).name);

    private void SetupConnectionEvents()
    {
        switch (NetworkManager.Instance.NetworkType)
        {
            case NetworkType.Server:
                ((ServerNetwork)NetworkManager.Instance.Network).Server.ClientConnected += (o, e) =>
                {
                    SendServerLoadSceneMessage(sceneToLoadName, e.Client.Id);
                };
                break;
            case NetworkType.Host:
                ((HostNetwork)NetworkManager.Instance.Network).Server.ClientConnected += (o, e) =>
                {
                    SendServerLoadSceneMessage(sceneToLoadName, e.Client.Id);
                };
                break;
        }
    }

    private World CreateNetworkWorld<TNetworkSystemAttribute>(string worldName, float tickPerSecond) where TNetworkSystemAttribute : NetworkSystemBaseAttribute
    {
        Debug.Log("Creating networked world");

        World world = new World(worldName, WorldFlags.None);

        DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, RemoveOtherNetworkSystems<TNetworkSystemAttribute>());

        //shadow wizard casting gang (we love casting objects)
        ((FixedStepSimulationSystemGroup)world.GetExistingSystemManaged(typeof(FixedStepSimulationSystemGroup))).Timestep = 1f / tickPerSecond;

        World.DefaultGameObjectInjectionWorld = world;

        Debug.Log(World.DefaultGameObjectInjectionWorld.Systems.Count);

        Debug.Log("created network world");

        return world;
    }

    private List<Type> RemoveOtherNetworkSystems<TNetworkSystemAttribute>() where TNetworkSystemAttribute : NetworkSystemBaseAttribute
    {
        var systems = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.All);

        if (typeof(TNetworkSystemAttribute).Equals(typeof(NetworkSystemBaseAttribute))) return (List<Type>)systems;

        List<Type> systemsToAdd = new List<Type>();

        foreach (Type system in systems)
        {
            bool foundNonDesirableSystem = false;

            foreach (var attribute in system.GetCustomAttributes(true))
            {
                if (!attribute.GetType().Equals(typeof(TNetworkSystemAttribute)) && typeof(NetworkSystemBaseAttribute).IsAssignableFrom(attribute.GetType()))
                {
                    foundNonDesirableSystem = true;
                    Debug.Log("system found");
                    Debug.Log(system);
                    break;
                }
            }

            if (!foundNonDesirableSystem) systemsToAdd.Add(system);
        }

        Debug.Log(systems.Count);
        Debug.Log(systemsToAdd.Count);

        return systemsToAdd;
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
        //if we're host, don't do this method
        if (clientId == NetworkManager.CLIENT_NET_ID) return;

        string sceneClientFinishedLoadingName = message.GetString();

        if (!sceneClientFinishedLoadingName.Equals(NetworkManager.Instance.NetworkSceneManager.sceneToLoadName))
        {
            Debug.LogWarning($"client sending completed load scene: {sceneClientFinishedLoadingName}, which is not the current scene: {SceneManager.GetActiveScene().name}");
            return;
        }

        CheckForDestroyedNetworkedSceneEntities(clientId);

        SendEntitySpawns(clientId);
    }

    [MessageHandler((ushort)NetworkMessageId.ServerLoadScene)]
    private static void ServerLoadScene(Message message)
    {
        //if we're host, don't do this method
        if (NetworkManager.Instance.NetworkType == NetworkType.Host) return;

        NetworkManager.Instance.NetworkSceneManager.LoadScene(message.GetString());
    }

    private static void CheckForDestroyedNetworkedSceneEntities(ushort clientId)
    {
        foreach (KeyValuePair<ulong, bool> activeNetworkedSceneEntityPair in NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.SceneEntitiesActive)
        {
            if (activeNetworkedSceneEntityPair.Value) continue;

            Message destroyEntityMessage = Message.Create(MessageSendMode.Reliable, NetworkMessageId.ServerDestroyEntity);

            destroyEntityMessage.AddULong(activeNetworkedSceneEntityPair.Key);

            NetworkManager.Instance.Network.SendMessage(destroyEntityMessage, SendMode.Server, clientId);
        }
    }

    private static void SendEntitySpawns(ushort clientId)
    {
        IEnumerator<KeyValuePair<ulong, Entity>> enumerator = NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.GetEntities();

        while (enumerator.MoveNext())
        {
            KeyValuePair<ulong, Entity> idEntityPair = enumerator.Current;

            if (NetworkManager.Instance.NetworkSceneManager.NetworkedEntityContainer.SceneEntitiesActive.ContainsKey(idEntityPair.Key)) continue;

            NetworkedEntityComponent networkedEntityComponent = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<NetworkedEntityComponent>(idEntityPair.Value);

            LocalTransform localTransform = NetworkManager.Instance.NetworkSceneManager.NetworkWorld.EntityManager.GetComponentData<LocalTransform>(idEntityPair.Value);

            NetworkManager.Instance.NetworkSceneManager.SendSpawnNetworkedEntityMessage(networkedEntityComponent.networkedPrefabHash, networkedEntityComponent.connectionId, localTransform,
                networkedEntityComponent.networkEntityId, clientId);
        }
    }
}