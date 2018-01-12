using IoTCoreDefaultApp.Utils;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;

namespace IoTCoreDefaultApp.Presenters
{
    public class WifiListViewItemPresenter : INotifyPropertyChanged
    {
        private TimeSpan expiredTimespan = TimeSpan.FromMinutes(5);

        public WifiListViewItemPresenter(WiFiAvailableNetwork availableNetwork, WiFiAdapter adapter, DateTime reportTime)
        {
            AvailableNetwork = availableNetwork;
            this.adapter = adapter;
            this.LastSeen = reportTime;
        }

        public async Task InitializeAsync()
        {
            if (!IsWpsPushButtonAvailable.HasValue)
            {
                IsWpsPushButtonAvailable = await Task<bool>.Run(async () =>
                {
                    return await IsWpsPushButtonAvailableAsync();
                });
            }
        }

        private WiFiAdapter adapter;
        public WiFiAdapter Adapter
        {
            get
            {
                return adapter;
            }
        }

        public void Disconnect()
        {
            adapter.Disconnect();
        }

        public bool? IsWpsPushButtonAvailable { get; set; }

        public bool NetworkKeyInfoVisibility
        {
            get
            {
                if ((AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Open80211 &&
                     AvailableNetwork.SecuritySettings.NetworkEncryptionType == NetworkEncryptionType.None) ||
                     IsEapAvailable)
                {
                    return false;
                }

                return true;
            }
        }

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

        private bool connectAutomatically = true;
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
                return availableNetwork.Ssid;
            }
        }

        public byte SignalBars
        {
            get
            {
                return AvailableNetwork.SignalBars;
            }
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

        public bool IsEapAvailable
        {
            get
            {
                Log.Trace($"{availableNetwork.SecuritySettings.NetworkAuthenticationType}");
                return ((availableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Rsna) ||
                    (availableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Wpa));
            }
        }

        public async Task<bool> IsWpsPushButtonAvailableAsync()
        {
            return await Task.Run(async () =>
            {
                await Task.CompletedTask;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5, 0))
                {
                    var task = adapter.GetWpsConfigurationAsync(availableNetwork).AsTask();
                    bool success = task.Wait(1000);
                    if (success)
                    {
                        if (task.Result.SupportedWpsKinds.Contains(WiFiWpsKind.PushButton))
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                    else
                    {
                        // GetWpsConfigurationAsync is not returning sometimes
                        // If the result isn't available let the user figure out if WPS is supported or not
                        Log.Trace($"GetWpsConfigurationAsync timed out: {availableNetwork.Ssid}");
                        return true;
                    }
                }
                return false;
            });
        }

        private WiFiAvailableNetwork availableNetwork;
        public WiFiAvailableNetwork AvailableNetwork
        {
            get
            {
                return availableNetwork;
            }

            private set
            {
                availableNetwork = value;
            }
        }

        private string message;
        public string Message
        {
            get
            {
                return message;
            }

            set
            {
                message = value;
                OnPropertyChanged("Message");
            }
        }

        private bool isMessageVisible = false;
        public bool IsMessageVisible
        {
            get
            {
                return isMessageVisible;
            }

            set
            {
                isMessageVisible = value;
                OnPropertyChanged("IsMessageVisible");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        public DateTime LastSeen { get; set; }
        public bool IsExpired
        {
            get
            {
                if (LastSeen + expiredTimespan < DateTime.Now)
                {
                    return true;
                }
                return false;
            }
        }
    }
}
