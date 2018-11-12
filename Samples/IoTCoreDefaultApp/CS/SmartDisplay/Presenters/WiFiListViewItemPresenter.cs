// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Devices.WiFi;
using Windows.Foundation.Metadata;
using Windows.Networking.Connectivity;

namespace SmartDisplay.Utils
{
    public class WiFiListViewItemPresenter : INotifyPropertyChanged
    {
        private TimeSpan _expiredTimespan = TimeSpan.FromMinutes(5);

        public WiFiAdapter Adapter { get; }

        private bool _usePassword = false;
        public bool UsePassword
        {
            get
            {
                return _usePassword;
            }
            set
            {
                _usePassword = value;
                NotifyPropertyChanged();
            }
        }

        private bool _connectAutomatically = true;
        public bool ConnectAutomatically
        {
            get
            {
                return _connectAutomatically;
            }
            set
            {
                _connectAutomatically = value;
                NotifyPropertyChanged();
            }
        }

        private string _userName;
        public string UserName
        {
            get
            {
                return _userName;
            }
            set
            {
                _userName = value;
                NotifyPropertyChanged();
            }
        }

        private string _password;
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                _password = value;
                NotifyPropertyChanged();
            }
        }

        private string _domain;
        public string Domain
        {
            get
            {
                return _domain;
            }
            set
            {
                _domain = value;
                NotifyPropertyChanged();
            }
        }

        public WiFiAvailableNetwork AvailableNetwork { get; }

        private string _message;
        public string Message
        {
            get
            {
                return _message;
            }

            set
            {
                _message = value;
                NotifyPropertyChanged();
            }
        }

        private bool _isMessageVisible = false;
        public bool IsMessageVisible
        {
            get
            {
                return _isMessageVisible;
            }

            set
            {
                _isMessageVisible = value;
                NotifyPropertyChanged();
            }
        }

        public bool? IsWpsPushButtonAvailable { get; set; }

        public string Ssid
        {
            get
            {
                return AvailableNetwork.Ssid;
            }
        }

        public byte SignalBars
        {
            get
            {
                return AvailableNetwork.SignalBars;
            }
        }

        public DateTime LastSeen { get; set; }

        public WiFiListViewItemPresenter(WiFiAvailableNetwork availableNetwork, WiFiAdapter adapter, DateTime reportTime)
        {
            AvailableNetwork = availableNetwork;
            Adapter = adapter;
            LastSeen = reportTime;
        }

        public async Task InitializeAsync()
        {
            if (!IsWpsPushButtonAvailable.HasValue)
            {
                IsWpsPushButtonAvailable = await Task.Run(async () =>
                {
                    return await IsWpsPushButtonAvailableAsync();
                });
            }
        }

        public void Disconnect()
        {
            Adapter.Disconnect();
        }

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

        public bool IsEapAvailable
        {
            get
            {
                App.LogService.Write($"{AvailableNetwork.SecuritySettings.NetworkAuthenticationType}");
                return ((AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Rsna) ||
                    (AvailableNetwork.SecuritySettings.NetworkAuthenticationType == NetworkAuthenticationType.Wpa));
            }
        }

        public async Task<bool> IsWpsPushButtonAvailableAsync()
        {
            return await Task.Run(async () =>
            {
                await Task.CompletedTask;
                if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5, 0))
                {
                    var task = Adapter.GetWpsConfigurationAsync(AvailableNetwork).AsTask();
                    bool success = task.Wait(1000);
                    if (success)
                    {
                        return task.Result.SupportedWpsKinds.Contains(WiFiWpsKind.PushButton);
                    }
                    else
                    {
                        // GetWpsConfigurationAsync is not returning sometimes
                        // If the result isn't available let the user figure out if WPS is supported or not
                        App.LogService.Write($"GetWpsConfigurationAsync timed out: {AvailableNetwork.Ssid}");
                        return true;
                    }
                }
                return false;
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
