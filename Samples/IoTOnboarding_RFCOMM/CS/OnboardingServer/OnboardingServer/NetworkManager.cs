using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Networking.Connectivity;
using Windows.Security.Credentials;

namespace OnboardingServer
{
    class NetworkManager
    {
        private WiFiAdapter m_wiFiAdapter = null;
        private IReadOnlyList<WiFiAdapter> m_wiFiAdapterList;
        private MainPage rootPage = null;

        public NetworkManager(MainPage mainPage)
        {
            rootPage = mainPage;
        }

        public async Task<int> Initialize()
        {
            if (m_wiFiAdapter == null)
            {
                //Request access of WiFi adapter.
                WiFiAccessStatus accessStatus = await WiFiAdapter.RequestAccessAsync();
                if (accessStatus != WiFiAccessStatus.Allowed)
                {
                    rootPage.Log("NETWORK_MANAGER::[ERROR]ScanForWiFiAdapterAsync: WiFi access denied.");
                }
                else
                {
                    //Find WiFi adatper
                    m_wiFiAdapterList = await WiFiAdapter.FindAllAdaptersAsync();
                    rootPage.Log("NETWORK_MANAGER::Found " + m_wiFiAdapterList.Count + " wifi adapter.");
                    while (m_wiFiAdapterList.Count < 1)
                    {
                        await System.Threading.Tasks.Task.Delay(3000);
                        m_wiFiAdapterList = await WiFiAdapter.FindAllAdaptersAsync();
                        rootPage.Log("NETWORK_MANAGER::Found " + m_wiFiAdapterList.Count+" wifi adapter.");
                    }

                    //Get the first WiFi adatper from the list. 
                    //TODO: Edit this part if the system has more than one WiFi adatpers.
                    m_wiFiAdapter = m_wiFiAdapterList[0];
                }
                return -1;
            }
            return 0;
        }

        /// <summary>
        /// Return the SSIDs of the available network list.
        /// </summary>
        public async Task<List<string>> GetNetworkList()
        {
            List<string> m_networkList = new List<string>();
            if (m_wiFiAdapter == null)
            {
                while (await Initialize() != 0){}
                return await GetNetworkList();
            }
            else
            {
                await m_wiFiAdapter.ScanAsync();
                if (m_wiFiAdapter.NetworkReport.AvailableNetworks.Count > 0)
                {
                    foreach (WiFiAvailableNetwork network in m_wiFiAdapter.NetworkReport.AvailableNetworks.Distinct())
                    {
                        rootPage.Log(string.Format("NETWORK_MANAGER::GetNetworkList: AP {0} found.", network.Ssid));
                        m_networkList.Add(network.Ssid);
                    }
                    return m_networkList.Distinct().ToList();
                }
            }
            return null;
        }

        /// <summary>
        /// Connects desired WiFi network specified by the input SSID.
        /// </summary>
        /// <param name="ssid">SSID of the desired WiFi network</param>
        /// <param name="password">Password of the desired WiFI network</param>
        public async Task<WiFiConnectionStatus> Connect(string ssid, string password)
        {
            rootPage.Log("NETWORK_MANAGER::Connecting to " + ssid + "...");

            try
            {
                foreach (WiFiAvailableNetwork network in m_wiFiAdapter.NetworkReport.AvailableNetworks)
                {
                    if (network.Ssid == ssid)
                    {
                        WiFiConnectionResult result = null;
                        if (network.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211)
                        {
                            result = await m_wiFiAdapter.ConnectAsync(network, WiFiReconnectionKind.Automatic);
                        }
                        else
                        {
                            PasswordCredential credential = new PasswordCredential();
                            credential.Password = password;
                            result = await m_wiFiAdapter.ConnectAsync(network, WiFiReconnectionKind.Automatic, credential);
                        }

                        rootPage.Log("NETWORK_MANAGER::Connection result: " + result.ConnectionStatus.ToString());
                        return result.ConnectionStatus;
                    }
                }
                rootPage.Log("NETWORK_MANAGER::Connection result: Network not found.");
                return WiFiConnectionStatus.NetworkNotAvailable;
            }
            catch (Exception e)
            {
                rootPage.Log("NETWORK_MANAGER::[ERROR] Hr" + e.HResult + ": " + e.Message);
            }
            return WiFiConnectionStatus.UnspecifiedFailure;
        }
    }
}
