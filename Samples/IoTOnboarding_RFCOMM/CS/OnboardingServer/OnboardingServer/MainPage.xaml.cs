using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Storage;
using Windows.Devices.WiFi;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace OnboardingServer
{
    public sealed partial class MainPage : Page
    {
        private char[] separator = new char[] { ';' };
        private NetworkManager m_wiFiManager;
        private WiFiConnectionStatus status = WiFiConnectionStatus.UnspecifiedFailure;
        private RFCommServer m_chatServer = null;
        private BluetoothManager m_bluetoothManager = null;

        public MainPage()
        {
            this.InitializeComponent();
            ConversationListBox.Items.VectorChanged += ConversationListScrollBottom;
            EasClientDeviceInformation eas = new EasClientDeviceInformation();
            Constants.SdpServiceName = eas.FriendlyName;

            m_wiFiManager = new NetworkManager(this);

            m_chatServer = new RFCommServer(this);
            m_chatServer.ClientConnected += OnChatServerClientConnect;
            m_chatServer.ClientDisonnected += OnChatServerClientDisconnect;
            m_chatServer.MessageReceived += OnChatServerMessageReceived;

            m_bluetoothManager = new BluetoothManager(this);

            //Check if there are existing WiFi Profile in the cache. If yes,
            //automatically attempts to connect to that WiF AP.
            var ignore = CheckWifiSetting();
        }

        private void ConversationListScrollBottom(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            if (ConversationListBox.Items.Count > 0)
            {
                ConversationListBox.ScrollIntoView(ConversationListBox.Items[ConversationListBox.Items.Count - 1]);
            }
        }

        #region CHAT_SERVER_EVENT_HANDLERS
        private void OnChatServerClientConnect(object sender, EventArgs e)
        {
            SendNetworkList();
        }

        private void OnChatServerClientDisconnect(object sender, EventArgs e)
        {
            Log("OnChatServerClientDisconnect");
            if (status != WiFiConnectionStatus.Success && !m_chatServer.isListening)
            {
                m_chatServer.StartListening();
                m_bluetoothManager.StartWatcher();
            }
        }

        private async void OnChatServerMessageReceived(object sender, RFCommServer.MessageReceivedEventArgs e)
        {
            string[] parsedMsg = e.Message.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if ("CONNECT" == parsedMsg[0])
            {
                string key = (parsedMsg.Length > 2) ? parsedMsg[2] : String.Empty;
                WiFiConnectionStatus status = await m_wiFiManager.Connect(parsedMsg[1], key);

                switch (status)
                {
                    case (WiFiConnectionStatus.Success):
                        m_chatServer.SendMessage("Successfully connected to " + parsedMsg[1] + ".");
                        m_chatServer.SendMessage("Disconnecting the RFComm Session");
                        SetWifiProfile(parsedMsg[1], key);
                        OnChatServerWiFiConnected();
                        break;
                    case (WiFiConnectionStatus.InvalidCredential):
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Invalid Credential", parsedMsg[1]));
                        break;
                    case (WiFiConnectionStatus.Timeout):
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Timeout", parsedMsg[1]));
                        break;
                    case (WiFiConnectionStatus.NetworkNotAvailable):
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Network Not Available", parsedMsg[1]));
                        SendNetworkList();
                        break;
                    case (WiFiConnectionStatus.AccessRevoked):
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Access Revoked", parsedMsg[1]));
                        break;
                    case (WiFiConnectionStatus.UnsupportedAuthenticationProtocol):
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Unsupported Authentication Protocol", parsedMsg[1]));
                        break;
                    default:
                        m_chatServer.SendMessage(string.Format("Failed to connected to {0}: Unspecified Failure", parsedMsg[1]));
                        break;
                }
            }
            else if ("REFRESH" == parsedMsg[0])
            {
                SendNetworkList();
            }
        }

        private void OnChatServerWiFiConnected()
        {
            m_bluetoothManager.StopWatcher();
            m_chatServer.Disconnect();
        }
        #endregion

        /// <summary>
        /// Sends the available network list to the client (aka the Manager Device PC).
        /// </summary>
        private async void SendNetworkList()
        {
            List<string> m_networkList = await m_wiFiManager.GetNetworkList();
            if (m_networkList != null)
            {
                string message = "NETWORKLIST";
                foreach (string ssid in m_networkList)
                {
                    message += separator[0];
                    message += ssid;
                }

                m_chatServer.SendMessage(message);
            }
            else
            {
                m_chatServer.SendMessage("[ERROR] Failed to retrieve available network list.");
            }
        }

        /// <summary>
        /// Check for existing wifi profile in the cache. If a WiFi profile exits, automatically connects
        /// to that WiFi AP.
        /// </summary>
        private async Task CheckWifiSetting()
        {
            try
            {
                var vault = new Windows.Security.Credentials.PasswordVault();
                var credentialList = vault.RetrieveAll();

                if (credentialList?.Count > 0)
                {
                    Log("Connecting to stored Wifi Profile...");
                    var credential = credentialList[0];
                    await m_wiFiManager.Initialize();
                    credential.RetrievePassword();
                    WiFiConnectionStatus status = await m_wiFiManager.Connect(credential.UserName, credential.Password);
                    if (status == WiFiConnectionStatus.Success)
                    {
                        Log("Connected to AP " + credential.UserName + ".");
                        return;
                    }

                    Log("Failed to connected to AP " + credential.UserName + ".");
                }

                m_chatServer.StartListening();
                m_bluetoothManager.StartWatcher();
            }
            catch(Exception e)
            {
                Log("CheckWifiSetting::[ERROR]" + e.Message);
            }
            return;
        }

        private void SetWifiProfile(string ssid, string password)
        {
            //Clear pre-existing WiFi Profiles.
            var vault = new Windows.Security.Credentials.PasswordVault();
            var credentialList = vault.RetrieveAll();

            foreach (var cred in credentialList)
            {
                vault.Remove(cred);
            }

            //Save current WiFi Profile
            vault.Add(new Windows.Security.Credentials.PasswordCredential("wifiProfile", ssid, password));
        }

        public void Log(string message)
        {
            string logmsg = DateTime.UtcNow.ToString() + "  " + message;
            ConversationListBox.Items.Add(logmsg);
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ConversationListBox.Items.Clear();
        }
    }
}
