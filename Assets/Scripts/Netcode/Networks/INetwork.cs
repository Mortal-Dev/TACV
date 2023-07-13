﻿using Riptide;

public interface INetwork
{
    public void Start(string[] args);

    public void Tick();

    public void Stop();

    public void SendMessage(Message message, SendMode sendMode = SendMode.None, ushort sendTo = ushort.MaxValue);
}