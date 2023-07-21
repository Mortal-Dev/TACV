using System.Collections.Generic;
using Unity.Entities;

public class ManagedNetworkSystems
{
    private static Dictionary<SystemHandle, NetworkType> systemHandles = new Dictionary<SystemHandle, NetworkType>();

    public static void AddManagedNetworkSystem(SystemHandle systemHandle, NetworkType networkType)
    {
        if (systemHandles.ContainsKey(systemHandle)) return;

        systemHandles.Add(systemHandle, networkType);
    }

    public static NetworkType GetNetworkSystemType(SystemHandle systemHandle)
    {
        if (systemHandles.TryGetValue(systemHandle, out NetworkType networkType))
        {
            return networkType;
        }

        return NetworkType.None;
    }
}