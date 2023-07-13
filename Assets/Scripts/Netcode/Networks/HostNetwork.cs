﻿using UnityEngine;
using Riptide;

public class HostNetwork : INetwork
{
    public Server Server { get; private set; }

    public Client Client { get; private set; }

    public void Start(string[] args)
    {
        Server = new Server();
        Server.RelayFilter = new MessageRelayFilter(typeof(NetworkMessageId));
        Server.Start(ushort.Parse(args[0]), ushort.Parse(args[1]));

        Client = new Client();
        Client.Connect("127.0.0.1" + ":" + args[0]);
    }

    public void Tick()
    {
        if (Server == null || Client == null)
        {
            Debug.LogError($"must call {nameof(Start)} before using {nameof(Tick)}");
            return;
        }

        Server.Update();
        Client.Update();
    }

    public void Stop()
    {
        Client.Disconnect();
        Server.Stop();

        Client = null;
        Server = null;
    }

    public void SendMessage(Message message, SendMode sendMode = SendMode.Client, ushort sendTo = ushort.MaxValue)
    {
        if (sendMode == SendMode.None)
        {
            Debug.LogError($"must specifcy {nameof(sendMode)} in SendMessage when hosting as either {nameof(SendMode.Server)} or {nameof(SendMode.Client)}");
            return;
        }

        if (sendMode == SendMode.Client)
        {
            Client.Send(message);
            return;
        }

        if (sendTo == ushort.MaxValue)
        {
            Server.SendToAll(message);
        }
        else
        {
            Server.Send(message, sendTo);
        }
    }
}

public enum SendMode
{
    None,
    Client,
    Server
}