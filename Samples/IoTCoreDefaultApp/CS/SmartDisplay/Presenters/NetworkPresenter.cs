// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.WiFi;
using Windows.Networking;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace SmartDisplay.Utils
{
    public class NetworkPresenter : IDisposable
    {
        private const uint EthernetIanaType = 6;
        private const uint WirelessInterfaceIanaType = 71;

        private Dictionary<string, WiFiAdapter> _wiFiAdapters = new Dictionary<string, WiFiAdapter>();
        private DeviceWatcher _wiFiAdaptersWatcher;
        private ManualResetEvent _enumAdaptersCompleted = new ManualResetEvent(false);

        private Dictionary<WiFiAvailableNetwork, WiFiAdapter> _networkNameToInfo;
        private static WiFiAccessStatus? _accessStatus;
        private TimeSpan _expiredTimespan = TimeSpan.FromMinutes(7);

        private ConcurrentDictionary<string, WiFiListViewItemPresenter> _availableNetworks = new ConcurrentDictionary<string, WiFiListViewItemPresenter>();
        private SemaphoreSlim _availableNetworksLock = new SemaphoreSlim(1, 1);

        public NetworkPresenter()
        {
            // WiFiAdapter.GetDeviceSelector and DeviceInformation.CreateWatcher can throw an exception if the user doesn't grant permission to access the WiFi adapter.
            try
            {
                _wiFiAdaptersWatcher = DeviceInformation.CreateWatcher(WiFiAdapter.GetDeviceSelector());
                _wiFiAdaptersWatcher.EnumerationCompleted += AdaptersEnumCompleted;
                _wiFiAdaptersWatcher.Added += AdaptersAdded;
                _wiFiAdaptersWatcher.Removed += AdaptersRemoved;
                _wiFiAdaptersWatcher.Start();
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
            }
        }

        private DateTime LastRefresh = DateTime.MinValue;
        public bool IsRefreshNeeded()
        {
            bool result = false;
            if (DateTime.Now > (LastRefresh + _expiredTimespan))
            {
                result = true;
            }
            return result;
        }

        private void AdaptersRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            _wiFiAdapters.Remove(args.Id);
        }

        private void AdaptersAdded(DeviceWatcher sender, DeviceInformation args)
        {
            _wiFiAdapters.Add(args.Id, null);
        }

        private async void AdaptersEnumCompleted(DeviceWatcher sender, object args)
        {
            List<string> WiFiAdaptersID = new List<string>(_wiFiAdapters.Keys);
            for (int i = 0; i < WiFiAdaptersID.Count; i++)
            {
                string id = WiFiAdaptersID[i];
                try
                {
                    _wiFiAdapters[id] = await WiFiAdapter.FromIdAsync(id);
                }
                catch (Exception)
                {
                    _wiFiAdapters.Remove(id);
                }
            }
            _enumAdaptersCompleted.Set();
        }

        public static string GetDirectConnectionName()
        {
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp?.NetworkAdapter?.IanaInterfaceType == EthernetIanaType)
                {
                    return icp.ProfileName;
                }
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
            }

            return null;
        }

        public static string GetCurrentNetworkName()
        {
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp != null)
                {
                    return icp.ProfileName;
                }
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
            }

            return $"<{Common.GetLocalizedText("NoInternetConnectionText")}>";
        }

        public static string GetCurrentIpv4Address()
        {
            try
            {
                var icp = NetworkInformation.GetInternetConnectionProfile();
                if (icp?.NetworkAdapter?.NetworkAdapterId != null)
                {
                    var name = icp.ProfileName;

                    foreach (var hostName in NetworkInformation.GetHostNames())
                    {
                        if (hostName.Type == HostNameType.Ipv4 &&
                            hostName.IPInformation?.NetworkAdapter?.NetworkAdapterId == icp.NetworkAdapter.NetworkAdapterId)
                        {
                            return hostName.CanonicalName;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
            }

            return $"<{Common.GetLocalizedText("NoInternetConnectionText")}>";
        }

        // Call this method before accessing WiFiAdapters Dictionary
        private async Task UpdateAdapters()
        {
            bool fInit = false;
            foreach (var adapter in _wiFiAdapters)
            {
                if (adapter.Value == null)
                {
                    // New Adapter plugged-in which requires Initialization
                    fInit = true;
                }
            }

            if (fInit)
            {
                List<string> WiFiAdaptersID = new List<string>(_wiFiAdapters.Keys);
                for (int i = 0; i < WiFiAdaptersID.Count; i++)
                {
                    string id = WiFiAdaptersID[i];
                    try
                    {
                        _wiFiAdapters[id] = await WiFiAdapter.FromIdAsync(id);
                    }
                    catch (Exception)
                    {
                        _wiFiAdapters.Remove(id);
                    }
                }
            }
        }
        public async Task<bool> WiFiIsAvailable()
        {
            if ((await TestAccess()) == false)
            {
                return false;
            }

            try
            {
                _enumAdaptersCompleted.WaitOne();
                await UpdateAdapters();
                return (_wiFiAdapters.Count > 0);
            }
            catch (Exception)
            {
                return false;
            }
        }

        private async Task<bool> UpdateInfo()
        {
            if ((await TestAccess()) == false)
            {
                return false;
            }

            _networkNameToInfo = new Dictionary<WiFiAvailableNetwork, WiFiAdapter>();
            List<WiFiAdapter> WiFiAdaptersList = new List<WiFiAdapter>(_wiFiAdapters.Values);
            foreach (var adapter in WiFiAdaptersList)
            {
                if (adapter == null)
                {
                    return false;
                }

                await adapter.ScanAsync();

                if (adapter.NetworkReport == null)
                {
                    continue;
                }

                foreach (var network in adapter.NetworkReport.AvailableNetworks)
                {
                    if (!HasSsid(_networkNameToInfo, network.Ssid))
                    {
                        _networkNameToInfo[network] = adapter;
                    }
                }
            }

            return true;
        }

        private bool HasSsid(Dictionary<WiFiAvailableNetwork, WiFiAdapter> resultCollection, string ssid)
        {
            foreach (var network in resultCollection)
            {
                if (!string.IsNullOrEmpty(network.Key.Ssid) && network.Key.Ssid == ssid)
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<IList<WiFiAvailableNetwork>> GetAvailableNetworks()
        {
            await UpdateInfo();

            return _networkNameToInfo.Keys.ToList();
        }

        public async Task<IList<WiFiListViewItemPresenter>> GetAvailableNetworks(bool refreshIfNeeded)
        {
            await UpdateAvailableNetworksAsync(refreshIfNeeded);

            try
            {
                await _availableNetworksLock.WaitAsync();
                var availableNetworks = _availableNetworks.Values.ToList();
                availableNetworks.Sort((item1, item2) => item2.AvailableNetwork.SignalBars.CompareTo(item1.AvailableNetwork.SignalBars));
                return availableNetworks;
            }
            finally
            {
                _availableNetworksLock.Release();
            }
        }

        public async Task<bool> UpdateAvailableNetworksAsync(bool refreshIfNeeded)
        {
            App.LogService.Write($"refreshIfNeeded={refreshIfNeeded}");
            try
            {
                await _availableNetworksLock.WaitAsync();

                if ((await TestAccess()) == false)
                {
                    return false;
                }

                if (refreshIfNeeded && !IsRefreshNeeded())
                {
                    return true;
                }

                LastRefresh = DateTime.Now;

                _enumAdaptersCompleted.WaitOne();
                List<WiFiAdapter> WiFiAdaptersList = new List<WiFiAdapter>(_wiFiAdapters.Values);
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
                        if (!string.IsNullOrWhiteSpace(network.Ssid))
                        {
                            if (_availableNetworks.ContainsKey(network.Ssid))
                            {
                                _availableNetworks.TryRemove(network.Ssid, out WiFiListViewItemPresenter value);
                            }

                            var item = new WiFiListViewItemPresenter(network, adapter, reportTime);
                            if (_availableNetworks.TryAdd(network.Ssid, item))
                            {
                                await item.InitializeAsync();
                                App.LogService.Write($"Adding {network.Ssid}");
                            }
                        }
                    }

                    // Remove some jitter from the list when refresh is repeatedly clicked
                    // by remembering networks from the last 5 minutes
                    DateTime expireTime = DateTime.Now - TimeSpan.FromMinutes(5);
                    foreach (var key in _availableNetworks.Keys)
                    {
                        if (_availableNetworks[key].LastSeen < expireTime)
                        {
                            _availableNetworks.TryRemove(key, out WiFiListViewItemPresenter value);
                        }
                    }
                }
                return true;
            }
            finally
            {
                _availableNetworksLock.Release();
            }
        }

        public void DisconnectNetwork(WiFiListViewItemPresenter network)
        {
            network.Adapter.Disconnect();
        }

        public WiFiListViewItemPresenter GetCurrentOOBENetworkName()
        {
            try
            {
                _availableNetworksLock.WaitAsync().ConfigureAwait(false);
                string connectedProfile = GetConnectedProfileName();
                if (connectedProfile != null)
                {
                    _availableNetworks.TryGetValue(connectedProfile, out WiFiListViewItemPresenter network);
                    return network;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                _availableNetworksLock.Release();
            }
        }

        static public string GetConnectedProfileName()
        {
            IReadOnlyCollection<ConnectionProfile> connectionProfiles = null;
            try
            {
                connectionProfiles = NetworkInformation.GetConnectionProfiles();
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
                return null;
            }

            if (connectionProfiles == null || connectionProfiles.Count < 1)
            {
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

            return firstProfile.ProfileName;
        }


        public static bool IsNetworkOpen(WiFiListViewItemPresenter network)
        {
            return network.AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None;
        }

        public WiFiListViewItemPresenter GetOOBECurrentWifiNetwork()
        {
            try
            {
                _availableNetworksLock.WaitAsync().ConfigureAwait(false);
                string connectedProfile = GetConnectedProfileName();
                if (connectedProfile != null)
                {
                    _availableNetworks.TryGetValue(connectedProfile, out WiFiListViewItemPresenter network);
                    return network;
                }
                else
                {
                    return null;
                }
            }
            finally
            {
                _availableNetworksLock.Release();
            }
        }

        public WiFiAvailableNetwork GetCurrentWiFiNetwork()
        {
            IReadOnlyCollection<ConnectionProfile> connectionProfiles = null;
            try
            {
                connectionProfiles = NetworkInformation.GetConnectionProfiles();
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
                return null;
            }

            if (connectionProfiles == null || connectionProfiles.Count < 1)
            {
                return null;
            }

            var validProfiles = connectionProfiles.Where(profile =>
            {
                return (profile.IsWlanConnectionProfile && profile.GetNetworkConnectivityLevel() != NetworkConnectivityLevel.None);
            });

            if (validProfiles.Count() < 1)
            {
                return null;
            }

            var firstProfile = validProfiles.First() as ConnectionProfile;

            return _networkNameToInfo.Keys.FirstOrDefault(wiFiNetwork => wiFiNetwork.Ssid.Equals(firstProfile.ProfileName));
        }

        public async Task<bool> ConnectToNetwork(WiFiAvailableNetwork network, bool autoConnect)
        {
            if (network == null)
            {
                return false;
            }

            if (_networkNameToInfo.TryGetValue(network, out var adapter))
            {
                var result = await adapter.ConnectAsync(
                    network,
                    autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual);
                return (result.ConnectionStatus == WiFiConnectionStatus.Success);
            }
            return false;
        }

        public async Task<bool> ConnectToNetworkWithPassword(WiFiAvailableNetwork network, bool autoConnect, PasswordCredential password)
        {
            if (network == null)
            {
                return false;
            }

            if (_networkNameToInfo.TryGetValue(network, out var adapter))
            {
                var result = await adapter.ConnectAsync(
                    network,
                    autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual,
                    password);
                return (result.ConnectionStatus == WiFiConnectionStatus.Success);
            }

            return false;
        }

        public void DisconnectNetwork(WiFiAvailableNetwork network)
        {
            if (network == null)
            {
                return;
            }

            if (_networkNameToInfo.TryGetValue(network, out var adapter))
            {
                adapter.Disconnect();
            }
        }

        public static bool IsNetworkOpen(WiFiAvailableNetwork network)
        {
            return network.SecuritySettings?.NetworkEncryptionType == NetworkEncryptionType.None;
        }

        private static async Task<bool> TestAccess()
        {
            if (!_accessStatus.HasValue)
            {
                _accessStatus = await WiFiAdapter.RequestAccessAsync();
            }

            return (_accessStatus == WiFiAccessStatus.Allowed);
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
            var networkList = new Dictionary<Guid, NetworkInfo>();

            try
            {
                var hostNamesList = NetworkInformation.GetHostNames();

                foreach (var hostName in hostNamesList)
                {
                    if (hostName.Type != HostNameType.Ipv4 && hostName.Type != HostNameType.Ipv6)
                    {
                        continue;
                    }

                    var adapter = hostName?.IPInformation?.NetworkAdapter;
                    if (adapter == null)
                    {
                        continue;
                    }

                    var profile = await adapter.GetConnectedProfileAsync();
                    if (profile == null)
                    {
                        continue;
                    }

                    if (!networkList.TryGetValue(adapter.NetworkAdapterId, out NetworkInfo info))
                    {
                        info = new NetworkInfo();
                        networkList[adapter.NetworkAdapterId] = info;

                        if (adapter.IanaInterfaceType == WirelessInterfaceIanaType &&
                            profile.ProfileName.Equals("Ethernet"))
                        {
                            info.NetworkName = "Wireless LAN Adapter";
                        }
                        else
                        {
                            info.NetworkName = profile.ProfileName;
                        }

                        var statusTag = profile.GetNetworkConnectivityLevel().ToString();
                        info.NetworkStatus = Common.GetLocalizedText("NetworkConnectivityLevel_" + statusTag);
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
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
            }

            return new List<NetworkInfo>(networkList.Values);
        }

        #region IDisposable
        bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    _enumAdaptersCompleted.Dispose();
                    if (_wiFiAdaptersWatcher != null)
                    {
                        _wiFiAdaptersWatcher.EnumerationCompleted -= AdaptersEnumCompleted;
                        _wiFiAdaptersWatcher.Added -= AdaptersAdded;
                        _wiFiAdaptersWatcher.Removed -= AdaptersRemoved;
                    }
                    _availableNetworksLock.Dispose();
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~NetworkPresenter()
        {
            Dispose(false);
        }
        #endregion
    }
}
