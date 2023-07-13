using Riptide.Utils;
using UnityEngine;

public class NetworkManager
{
    public static NetworkManager Instance
    {
        get
        {
            if (networkManager == null) networkManager = new NetworkManager();

            return networkManager;
        }
    }

    private static NetworkManager networkManager;

    public NetworkType NetworkType { get; private set; } = NetworkType.None;

    public NetworkSceneManager NetworkSceneManager { get; private set; }

    public INetwork Network { get; private set; }

    public const ushort SERVER_NET_ID = ushort.MaxValue;

    public static ushort CLIENT_NET_ID { get; private set; }

    public void Tick() => Network?.Tick();
    
    public NetworkManager()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);
    }

    public void StartClient(string connection, int ticksPerSecond = 30)
    {
        if (NetworkType == NetworkType.None) NetworkType = NetworkType.Client;

        Network = new ClientNetwork();
        Network.Start(new string[] { connection });

        ((ClientNetwork)Network).Client.Connected += (s, e) => CLIENT_NET_ID = ((ClientNetwork)Network).Client.Id;

        NetworkSceneManager = new NetworkSceneManager(ticksPerSecond);
    }

    public void StartServer(ushort port, ushort maxPlayerCount, int ticksPerSecond = 30)
    {
        if (NetworkType == NetworkType.None) NetworkType = NetworkType.Server;

        Network = new ServerNetwork();
        Network.Start(new string[] { port.ToString(), maxPlayerCount.ToString() });

        NetworkSceneManager = new NetworkSceneManager(ticksPerSecond);
    }

    public void StartHost(ushort port, ushort maxPlayerCount, int ticksPerSecond = 30)
    {
        if (NetworkType == NetworkType.None) NetworkType = NetworkType.Host;

        Network = new HostNetwork();
        Network.Start(new string[] { port.ToString(), maxPlayerCount.ToString() });

        ((HostNetwork)Network).Client.Connected += (s, e) => CLIENT_NET_ID = ((HostNetwork)Network).Client.Id;

        NetworkSceneManager = new NetworkSceneManager(ticksPerSecond);
    }

    public void Stop()
    {
        Network.Stop();
        Network = null;
        CLIENT_NET_ID = 0;
        NetworkSceneManager = null;
    }
}