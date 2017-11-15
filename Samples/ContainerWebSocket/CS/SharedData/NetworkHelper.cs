using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SharedData
{
    public class NetworkHelper
    {
        public static string GetLocalHost()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                {
                    if ((unicast.PrefixOrigin == PrefixOrigin.Dhcp) &&
                        (unicast.Address.AddressFamily == AddressFamily.InterNetwork))
                    {
                        return unicast.Address.ToString();
                    }
                }
            }
            throw new SharedDataException("Local host address not found");
        }

        public static string GetDockerNAT()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                if (adapter.Name.IndexOf("nat", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    foreach (var unicast in adapter.GetIPProperties().UnicastAddresses)
                    {
                        if (unicast.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return unicast.Address.ToString();
                        }
                    }
                }
            }
            throw new SharedDataException("Docker nat address not found");
        }

        public static string GetGateWayAddress()
        {
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();
            foreach (var adapter in adapters)
            {
                foreach (var gateway in adapter.GetIPProperties().GatewayAddresses)
                {
                    if (gateway.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return gateway.Address.ToString();
                   }
                }
            }
            throw new SharedDataException("Gateway address not found");
        }

    }
}
