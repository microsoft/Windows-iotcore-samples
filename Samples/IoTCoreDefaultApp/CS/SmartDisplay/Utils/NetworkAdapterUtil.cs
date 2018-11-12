// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace SmartDisplay.Utils
{
    public static class NetworkAdapterUtil
    {
        public static List<AdapterConfig> GetAdapters()
        {
            var adapterList = new List<AdapterConfig>();
            var netInterfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (var netInterface in netInterfaces)
            {
                var ipProps = netInterface.GetIPProperties();
                var ipV4Info = ipProps.UnicastAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);
                var gatewayIpV4Info = ipProps.GatewayAddresses.FirstOrDefault(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                var physicalAddressBytes = netInterface.GetPhysicalAddress().GetAddressBytes();
                var physicalAddressString = string.Join("-", Enumerable.Range(0, physicalAddressBytes.Length).Select(s => string.Format("{0:X2}", physicalAddressBytes[s])));

                var dnsString = string.Empty;
                ipProps.DnsAddresses?.ToList()
                    .Where(x => x.AddressFamily == AddressFamily.InterNetwork).ToList()
                    .ForEach(x => dnsString += (x.ToString() + Environment.NewLine));

                adapterList.Add(new AdapterConfig
                {
                    Name = netInterface.Name,
                    Description = netInterface.Description,
                    Type = netInterface.NetworkInterfaceType.ToString(),
                    PhysicalAddress = physicalAddressString,
                    IPv4Address = ipV4Info?.Address.ToString(),
                    SubnetMask = ipV4Info?.IPv4Mask.ToString(),
                    GatewayAddress = gatewayIpV4Info?.Address.ToString(),
                    DHCPServer = ipProps.DhcpServerAddresses.FirstOrDefault()?.ToString(),
                    DNS = dnsString.Trim()
                });
            }

            return adapterList;
        }
    }

    public class AdapterConfig
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string Type { get; set; }
        public string PhysicalAddress { get; set; }
        public string IPv4Address { get; set; }
        public string SubnetMask { get; set; }
        public string GatewayAddress { get; set; }
        public string DHCPServer { get; set; }
        public string DNS { get; set; }
    }
}
