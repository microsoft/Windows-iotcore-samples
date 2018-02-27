// Copyright (c) Microsoft. All rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml.Media.Imaging;

namespace WiFiConnect
{
    public class WiFiNetworkDisplay : INotifyPropertyChanged
    {
        private WiFiAdapter adapter;
        public WiFiNetworkDisplay(WiFiAvailableNetwork availableNetwork, WiFiAdapter adapter)
        {
            AvailableNetwork = availableNetwork;
            this.adapter = adapter;
        }

        public async void Update()
        {
            UpdateWiFiImage();
            UpdateNetworkKeyVisibility();
            UpdateHiddenSsidTextBoxVisibility();
            await UpdateConnectivityLevelAsync();
            await UpdateWpsPushButtonAvailableAsync();
        }

        private void UpdateHiddenSsidTextBoxVisibility()
        {
            IsHiddenNetwork = string.IsNullOrEmpty(AvailableNetwork.Ssid);
            OnPropertyChanged("IsHiddenNetwork");
        }

        private void UpdateNetworkKeyVisibility()
        {
            // Only show the password box if needed
            if ((AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211 &&
                 AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None) ||
                 IsEapAvailable)
            {
                NetworkKeyInfoVisibility = false;
            }
            else
            {
                NetworkKeyInfoVisibility = true;
            }
        }

        private void UpdateWiFiImage()
        {
            string imageFileNamePrefix = "secure";
            if (AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211)
            {
                imageFileNamePrefix = "open";
            }

            string imageFileName = string.Format("ms-appx:/Assets/{0}_{1}bar.png", imageFileNamePrefix, AvailableNetwork.SignalBars);

            WiFiImage = new BitmapImage(new Uri(imageFileName));

            OnPropertyChanged("WiFiImage");

        }

        public async Task UpdateConnectivityLevelAsync()
        {
            string connectivityLevel = "Not Connected";
            string connectedSsid = null;

            var connectedProfile = await adapter.NetworkAdapter.GetConnectedProfileAsync();
            if (connectedProfile != null &&
                connectedProfile.IsWlanConnectionProfile &&
                connectedProfile.WlanConnectionProfileDetails != null)
            {
                connectedSsid = connectedProfile.WlanConnectionProfileDetails.GetConnectedSsid();
            }

            if (!string.IsNullOrEmpty(connectedSsid))
            {
                if (connectedSsid.Equals(AvailableNetwork.Ssid) ||
                    connectedSsid.Equals(HiddenSsid))
                {
                    connectivityLevel = connectedProfile.GetNetworkConnectivityLevel().ToString();
                }
            }

            ConnectivityLevel = connectivityLevel;
            OnPropertyChanged("ConnectivityLevel");
        }

        public async Task UpdateWpsPushButtonAvailableAsync()
        {
            IsWpsPushButtonAvailable = await IsWpsPushButtonAvailableAsync();
            OnPropertyChanged("IsWpsPushButtonAvailable");
        }

        public void Disconnect()
        {
            adapter.Disconnect();
        }

        public bool IsWpsPushButtonAvailable { get; set; }

        public bool NetworkKeyInfoVisibility { get; set; }

        public bool IsHiddenNetwork { get; set; }

        private bool usePassword = false;
        public bool UsePassword
        {
            get
            {
                return usePassword;
            }
            set
            {
                usePassword = value;
                OnPropertyChanged("UsePassword");
            }
        }

        private bool connectAutomatically = false;
        public bool ConnectAutomatically
        {
            get
            {
                return connectAutomatically;
            }
            set
            {
                connectAutomatically = value;
                OnPropertyChanged("ConnectAutomatically");
            }
        }

        public String Ssid
        {
            get
            {
                return string.IsNullOrEmpty(AvailableNetwork.Ssid) ? "Hidden Network" : AvailableNetwork.Ssid;
            }
        }

        public String Bssid
        {
            get
            {
                return AvailableNetwork.Bssid;

            }
        }

        public String ChannelCenterFrequency
        {
            get
            {
                return string.Format("{0}kHz", AvailableNetwork.ChannelCenterFrequencyInKilohertz);
            }
        }

        public String Rssi
        {
            get
            {
                return string.Format("{0}dBm", AvailableNetwork.NetworkRssiInDecibelMilliwatts);
            }
        }

        public String SecuritySettings
        {
            get
            {
                return string.Format("Authentication: {0}; Encryption: {1}", AvailableNetwork.SecuritySettings.NetworkAuthenticationType, AvailableNetwork.SecuritySettings.NetworkEncryptionType);
            }
        }

        public String ConnectivityLevel
        {
            get;
            private set;
        }

        public BitmapImage WiFiImage
        {
            get;
            private set;
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { userName = value; OnPropertyChanged("UserName"); }
        }

        private string password;
        public string Password
        {
            get { return password; }
            set { password = value; OnPropertyChanged("Password"); }
        }

        private string domain;
        public string Domain
        {
            get { return domain; }
            set { domain = value; OnPropertyChanged("Domain"); }
        }

        private string hiddenSsid;
        public string HiddenSsid
        {
            get { return hiddenSsid; }
            set { hiddenSsid = value; OnPropertyChanged("HiddenSsid"); }
        }

        public bool IsEapAvailable
        {
            get
            {
                return ((AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Rsna) ||
                    (AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Wpa));
            }
        }

        public async Task<bool> IsWpsPushButtonAvailableAsync()
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5, 0))
            {
                var result = await adapter.GetWpsConfigurationAsync(AvailableNetwork);
                if (result.SupportedWpsKinds.Contains(WiFiWpsKind.PushButton))
                    return true;
            }

            return false;
        }
        public WiFiAvailableNetwork AvailableNetwork { get; private set; }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
