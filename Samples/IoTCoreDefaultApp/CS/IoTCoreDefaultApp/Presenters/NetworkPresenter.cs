// Copyright (c) Microsoft. All rights reserved.


using IoTCoreDefaultApp.Presenters;
using IoTCoreDefaultApp.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;
using Windows.System.Threading;

namespace IoTCoreDefaultApp
{
    public class NetworkPresenter
    {
        private readonly static uint EthernetIanaType = 6;
        private readonly static uint WirelessInterfaceIanaType = 71;
        private Dictionary<String, WiFiAdapter> WiFiAdapters = new Dictionary<string, WiFiAdapter>();
        private DeviceWatcher WiFiAdaptersWatcher;
        ManualResetEvent EnumAdaptersCompleted = new ManualResetEvent(false);

        private ConcurrentDictionary<string, WifiListViewItemPresenter> AvailableNetworks = new ConcurrentDictionary<string, WifiListViewItemPresenter>();
        private SemaphoreSlim AvailableNetworksLock = new SemaphoreSlim(1, 1);

        private static WiFiAccessStatus? accessStatus;
        private ThreadPoolTimer wifiRefreshTimer;
        private TimeSpan refreshTimespan = TimeSpan.FromMinutes(5);
        private TimeSpan expiredTimespan = TimeSpan.FromMinutes(7);

        public NetworkPresenter()
        {
            Log.Enter();
            Task.Run(() =>
            {
                if (TestAccess().Result)
                {
                    WiFiAdaptersWatcher = DeviceInformation.CreateWatcher(WiFiAdapter.GetDeviceSelector());
                    WiFiAdaptersWatcher.EnumerationCompleted += AdaptersEnumCompleted;
                    WiFiAdaptersWatcher.Added += AdaptersAdded;
                    WiFiAdaptersWatcher.Removed += AdaptersRemoved;
                    WiFiAdaptersWatcher.Start();
                }
            });
            Log.Leave();
        }

        private void AdaptersRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            Log.Enter();
            WiFiAdapters.Remove(args.Id);
            Log.Leave();
        }

        private void AdaptersAdded(DeviceWatcher sender, DeviceInformation args)
        {
            Log.Enter();
            WiFiAdapters.Add(args.Id, null);
            Log.Leave();
        }

        private async void AdaptersEnumCompleted(DeviceWatcher sender, object args)
        {
            Log.Enter();
            List<String> WiFiAdaptersID = new List<string>(WiFiAdapters.Keys);
            for(int i = 0; i < WiFiAdaptersID.Count; i++)
            {
                string id = WiFiAdaptersID[i];
                try
                {
                    WiFiAdapters[id] = await WiFiAdapter.FromIdAsync(id);
                }
                catch (Exception)
                {
                    WiFiAdapters.Remove(id);
                }
            }
            EnumAdaptersCompleted.Set();
            if (WiFiAdapters.Count() > 0)
            {
                wifiRefreshTimer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, refreshTimespan);
            }
            Log.Leave();
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            Log.Enter();
            App.NetworkPresenter.UpdateAvailableNetworksAsync(false).Wait();
            Log.Leave();
        }

        public static string GetDirectConnectionName()
        {
            Log.Enter();
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp != null && icp.NetworkAdapter != null && icp.NetworkAdapter.IanaInterfaceType == EthernetIanaType)
                {
                    return icp.ProfileName;
                }
            }
            catch (Exception)
            {
                // do nothing
                // seeing cases where NetworkInformation.GetInternetConnectionProfile() fails
            }

            Log.Leave();
            return null;
        }

        public static string GetCurrentNetworkName()
        {
            Log.Enter();
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp != null)
                {
                    return icp.ProfileName;
                }
            }
            catch (Exception)
            {
                // do nothing
                // seeing cases where NetworkInformation.GetInternetConnectionProfile() fails
            }

            var resourceLoader = ResourceLoader.GetForCurrentView();
            var msg = resourceLoader.GetString("NoInternetConnection");
            Log.Leave();
            return msg;
        }

        public static string GetCurrentIpv4Address()
        {
            Log.Enter();
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp != null && icp.NetworkAdapter != null && icp.NetworkAdapter.NetworkAdapterId != null)
                {
                    var name = icp.ProfileName;

                        var hostnames = NetworkInformation.GetHostNames();

                        foreach (var hn in hostnames)
                        {
                            if (hn.IPInformation != null &&
                                hn.IPInformation.NetworkAdapter != null &&
                                hn.IPInformation.NetworkAdapter.NetworkAdapterId != null &&
                                hn.IPInformation.NetworkAdapter.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId &&
                                hn.Type == HostNameType.Ipv4)
                            {
                                return hn.CanonicalName;
                            }
                        }
                    }
                }
            catch (Exception)
            {
                // do nothing
                // in some (strange) cases NetworkInformation.GetHostNames() fails... maybe a bug in the API...
            }

            var resourceLoader = ResourceLoader.GetForCurrentView();
            var msg = resourceLoader.GetString("NoInternetConnection");
            Log.Leave();
            return msg;
        }

        // Call this method before accessing WiFiAdapters Dictionary
        private async Task UpdateAdapters()
        {
            Log.Enter();
            bool fInit = false;
            foreach (var adapter in WiFiAdapters)
            {
                if (adapter.Value == null)
                {
                    // New Adapter plugged-in which requires Initialization
                    fInit = true;
                }
            }

            if (fInit)
            {
                List<String> WiFiAdaptersID = new List<string>(WiFiAdapters.Keys);
                for (int i = 0; i < WiFiAdaptersID.Count; i++)
                {
                    string id = WiFiAdaptersID[i];
                    try
                    {
                        WiFiAdapters[id] = await WiFiAdapter.FromIdAsync(id);
                    }
                    catch (Exception)
                    {
                        WiFiAdapters.Remove(id);
                    }
                }
            }
            Log.Leave();

        }
        public async Task<bool> WifiIsAvailable()
        {
            Log.Enter();
            if ((await TestAccess()) == false)
            {
                return false;
            }

            try
            {
                EnumAdaptersCompleted.WaitOne();
                if (WiFiAdapters.Count == 0)
                {
                    await UpdateAdapters();
                }
                Log.Leave();
                return (WiFiAdapters.Count > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private DateTime LastRefresh = DateTime.MinValue;
        public bool IsRefreshNeeded()
        {
            Log.Enter();
            bool result = false;
            if (DateTime.Now > (LastRefresh + expiredTimespan))
            {
                result = true;
            }
            Log.Leave($"result={result}");
            return result;
        }

        public async Task<bool> UpdateAvailableNetworksAsync(bool refreshIfNeeded)
        {
            Log.Enter($"refreshIfNeeded={refreshIfNeeded}");
            try
            {
                await AvailableNetworksLock.WaitAsync();

                if ((await TestAccess()) == false)
                {
                    return false;
                }

                if (refreshIfNeeded && !IsRefreshNeeded())
                {
                    return true;
                }

                LastRefresh = DateTime.Now;

                EnumAdaptersCompleted.WaitOne();
                List<WiFiAdapter> WiFiAdaptersList = new List<WiFiAdapter>(WiFiAdapters.Values);
                foreach (var adapter in WiFiAdaptersList)
                {
                    if (adapter == null)
                    {
                        return false;
                    }

                    try
                    {
                        await adapter.ScanAsync();
                    }
                    catch (Exception)
                    {
                        // ScanAsync() can throw an exception if the scan timeouts.
                        continue;
                    }

                    if (adapter.NetworkReport == null)
                    {
                        continue;
                    }

                    DateTime reportTime = DateTime.Now;
                    foreach (var network in adapter.NetworkReport.AvailableNetworks)
                    {
                        if (!String.IsNullOrWhiteSpace(network.Ssid))
                        {
                            if (AvailableNetworks.ContainsKey(network.Ssid))
                            {
                                WifiListViewItemPresenter value;
                                AvailableNetworks.TryRemove(network.Ssid, out value);
                            }

                            var item = new WifiListViewItemPresenter(network, adapter, reportTime);
                            if (AvailableNetworks.TryAdd(network.Ssid, item))
                            {
                                await item.InitializeAsync();
                                Log.Trace($"Adding {network.Ssid}");
                            }
                        }
                    }

                    // remove some jitter from the list when refresh is repeatedly clicked
                    // by remembering networks from the last 5 minutes
                    DateTime expireTime = DateTime.Now - TimeSpan.FromMinutes(5);
                    foreach(var key in AvailableNetworks.Keys)
                    {
                        if (AvailableNetworks[key].LastSeen < expireTime)
                        {
                            WifiListViewItemPresenter value;
                            AvailableNetworks.TryRemove(key, out value);
                        }
                    }
                }

                Log.Leave();
                return true;
            }
            finally
            {
                AvailableNetworksLock.Release();
            }
        }

        public async Task<IList<WifiListViewItemPresenter>> GetAvailableNetworks(bool refreshIfNeeded)
        {
            Log.Enter();
            await UpdateAvailableNetworksAsync(refreshIfNeeded);

            try
            {
                await AvailableNetworksLock.WaitAsync();
                var availableNetworks = AvailableNetworks.Values.ToList();
                availableNetworks.Sort((item1, item2) => item2.AvailableNetwork.SignalBars.CompareTo(item1.AvailableNetwork.SignalBars));
                return availableNetworks;
            }
            finally
            {
                AvailableNetworksLock.Release();
            }
        }

        static public string GetConnectedProfileName()
        {
            Log.Enter();
            IReadOnlyCollection<ConnectionProfile> connectionProfiles = null;
            try
            {
                connectionProfiles = NetworkInformation.GetConnectionProfiles();

                if (connectionProfiles == null || connectionProfiles.Count < 1)
                {
                    return null;
                }
            }
            catch (Exception)
            {
                // seeing cases where NetworkInformation calls fail
                return null;
            }

            var validProfiles = connectionProfiles.Where(profile =>
            {
                return (profile.IsWlanConnectionProfile && profile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None);
            });

            if (validProfiles == null || validProfiles.Count() < 1)
            {
                return null;
            }

            var firstProfile = validProfiles.First() as ConnectionProfile;

            Log.Leave();
            return firstProfile.ProfileName;
        }

        public WifiListViewItemPresenter GetCurrentWifiNetwork()
        {
            Log.Enter();


            try
            {
                AvailableNetworksLock.WaitAsync().ConfigureAwait(false);
                string connectedProfile = GetConnectedProfileName();
                if (connectedProfile != null)
                {
                    WifiListViewItemPresenter network;
                    AvailableNetworks.TryGetValue(connectedProfile, out network);
                    Log.Leave();
                    return network;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                AvailableNetworksLock.Release();
            }
        }

        public async Task<WiFiConnectionStatus> ConnectToNetwork(WifiListViewItemPresenter network, bool autoConnect)
        {
            Log.Enter();
            await AvailableNetworksLock.WaitAsync();
            if (network == null)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }

            try
            {
                var result = await network.Adapter.ConnectAsync(network.AvailableNetwork, autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual);

                //Call redirect only for Open Wifi
                if (IsNetworkOpen(network))
                {
                    //Navigate to http://www.msftconnecttest.com/redirect 
                    NavigationUtils.NavigateToScreen(typeof(WebBrowserPage), Common.GetResourceText("MicrosoftWifiConnect"));
                }

                Log.Leave($"LEAVE {result.ConnectionStatus}");
                return result.ConnectionStatus;
            }
            catch (Exception)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }
        }

        public void DisconnectNetwork(WifiListViewItemPresenter network)
        {
            Log.Enter();
            network.Adapter.Disconnect();
            Log.Leave();

        }

        public static bool IsNetworkOpen(WifiListViewItemPresenter network)
        {
            Log.Enter();
            return network.AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None;
        }

        public async Task<WiFiConnectionStatus> ConnectToNetworkWithPassword(WifiListViewItemPresenter network, bool autoConnect, PasswordCredential password)
        {
            Log.Enter();
            try
            {
                var result = await network.Adapter.ConnectAsync(
                    network.AvailableNetwork,
                    autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual,
                    password);

                Log.Leave($"LEAVE {result.ConnectionStatus}");
                return result.ConnectionStatus;
            }
            catch (Exception)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }
        }

        private static async Task<bool> TestAccess()
        {
            Log.Enter();
            if (!accessStatus.HasValue)
            {
                accessStatus = await WiFiAdapter.RequestAccessAsync();
            }

            Log.Leave();
            return (accessStatus == WiFiAccessStatus.Allowed);
        }


        public class NetworkInfo
        {
            public string NetworkName { get; set; }
            public string NetworkIpv6 { get; set; }
            public string NetworkIpv4 { get; set; }
            public string NetworkStatus { get; set; }
        }

        public static async Task<IList<NetworkInfo>> GetNetworkInformation()
        {
            Log.Enter();
            var networkList = new Dictionary<Guid, NetworkInfo>();

            try
            {
                var hostNamesList = NetworkInformation.GetHostNames();
                var resourceLoader = ResourceLoader.GetForCurrentView();

                foreach (var hostName in hostNamesList)
                {
                    if (hostName.Type == HostNameType.Ipv4 || hostName.Type == HostNameType.Ipv6)
                    {
                        NetworkInfo info = null;
                        if (hostName.IPInformation != null && hostName.IPInformation.NetworkAdapter != null)
                        {
                            var profile = await hostName.IPInformation.NetworkAdapter.GetConnectedProfileAsync();
                            if (profile != null)
                            {
                                var found = networkList.TryGetValue(hostName.IPInformation.NetworkAdapter.NetworkAdapterId, out info);
                                if (!found)
                                {
                                    info = new NetworkInfo();
                                    networkList[hostName.IPInformation.NetworkAdapter.NetworkAdapterId] = info;

                                    // NetworkAdapter API does not provide a way to tell if this is a physical adapter or virtual one; e.g. soft AP
                                    // So, provide heuristics to check for virtual network adapter
                                    if ((hostName.IPInformation.NetworkAdapter.IanaInterfaceType == WirelessInterfaceIanaType &&
                                        profile.ProfileName.Equals("Ethernet")) ||
                                        (hostName.IPInformation.NetworkAdapter.IanaInterfaceType == WirelessInterfaceIanaType &&
                                        hostName.IPInformation.NetworkAdapter.InboundMaxBitsPerSecond == 0 &&
                                        hostName.IPInformation.NetworkAdapter.OutboundMaxBitsPerSecond == 0)
                                        )
                                    {
                                        info.NetworkName = resourceLoader.GetString("VirtualNetworkAdapter");
                                    }
                                    else
                                    {
                                        info.NetworkName = profile.ProfileName;
                                    }
                                    var statusTag = profile.GetNetworkConnectivityLevel().ToString();
                                    info.NetworkStatus = resourceLoader.GetString("NetworkConnectivityLevel_" + statusTag);
                                }
                            }
                        }

                        // No network adapter was found. So, assign the network info to a virtual adapter header
                        if (info == null)
                        {
                            info = new NetworkInfo();
                            info.NetworkName = resourceLoader.GetString("VirtualNetworkAdapter");
                            // Assign a new GUID, since we don't have a network adapter
                            networkList[Guid.NewGuid()] = info;
                            info.NetworkStatus = resourceLoader.GetString("NetworkConnectivityLevel_LocalAccess");
                        }

                        if (hostName.Type == HostNameType.Ipv4)
                        {
                            info.NetworkIpv4 = hostName.CanonicalName;
                        }
                        else
                        {
                            info.NetworkIpv6 = hostName.CanonicalName;
                        }
                    }
                }
            }
            catch (Exception)
            {
                // do nothing
                // in some (strange) cases NetworkInformation.GetHostNames() fails... maybe a bug in the API...
            }

            var res = new List<NetworkInfo>();
            res.AddRange(networkList.Values);
            Log.Leave();
            return res;
        }
    }
}
