// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Networking.Connectivity;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.ViewModels
{
    public class DeviceInfoPageVM : BaseViewModel
    {
        #region UI properties

        public string DeviceName
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public string IPAddress
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public string BoardName
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public BitmapImage BoardImage
        {
            get { return GetStoredProperty<BitmapImage>(); }
            private set { SetStoredProperty(value); }
        }
        public string OSVersion
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public string NetworkName
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public string AzureHub
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public string AzureDeviceId
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }
        public bool IsIoTHubAvailable
        {
            get { return IoTHubService?.IsDeviceClientConnected == true; }
        }
        public string AppBuild
        {
            get { return GetStoredProperty<string>(); }
            private set { SetStoredProperty(value); }
        }

        public ObservableCollection<NetworkInfoDataTemplate> NetworkCollection
        {
            get { return GetStoredProperty<ObservableCollection<NetworkInfoDataTemplate>>(); }
            set { SetStoredProperty(value); }
        }

        public ObservableCollection<string> DevicesCollection
        {
            get { return GetStoredProperty<ObservableCollection<string>>(); }
            set { SetStoredProperty(value); }
        }

        public bool MakerImageBannerVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public int SubtitleFontSize
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }

        public LanguageManager LanguageManager { get; } = LanguageManager.GetInstance();

        #endregion

        private IIoTHubService IoTHubService { get; set; }

        public DeviceInfoPageVM() : base()
        {
            NetworkCollection = new ObservableCollection<NetworkInfoDataTemplate>();
            SubtitleFontSize = 16;
        }

        public async Task<bool> SetUpVM()
        {
            IoTHubService = AppService?.GetRegisteredService<IIoTHubService>();

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;
            try
            {
                UpdateBoardInfo();
                await UpdateNetworkInfo();
                UpdateConnectedDevices();
                MakerImageBannerVisible = await ProcessLauncherUtil.GetIsMakerImageAsync();
                return true;
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                return false;
            }
        }

        public void TearDownVM()
        {
            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
        }

        private void NetworkInformation_NetworkStatusChanged(object sender)
        {
            InvokeOnUIThread(async () => { await UpdateNetworkInfo(); });
        }

        private void UpdateBoardInfo()
        {
            BoardName = DeviceInfoPresenter.GetBoardName();
            BoardImage = new BitmapImage(DeviceInfoPresenter.GetBoardImageUri());
            OSVersion = Common.GetOSVersionString();
        }

        private async Task<bool> UpdateNetworkInfo()
        {
            DeviceName = DeviceInfoPresenter.GetDeviceName();
            IPAddress = NetworkPresenter.GetCurrentIpv4Address();
            NetworkName = NetworkPresenter.GetCurrentNetworkName();
            AppBuild = Common.GetAppVersion();

            AzureHub = IoTHubService?.HostName ?? Common.GetLocalizedText("NotAvailable");
            AzureDeviceId = IoTHubService?.DeviceId ?? Common.GetLocalizedText("NotAvailable");

            var networkInfoList = await NetworkPresenter.GetNetworkInformation();

            if (networkInfoList != null)
            {
                NetworkCollection.Clear();
                foreach (var network in networkInfoList)
                {
                    NetworkCollection.Add(new NetworkInfoDataTemplate(network));
                }
                return true;
            }
            return false;
        }

        private void UpdateConnectedDevices()
        {
            var connectedDevicePresenter = new ConnectedDevicePresenter(Window.Current?.Dispatcher);
            DevicesCollection = connectedDevicePresenter.GetConnectedDevices();
        }

        #region Commands
        private RelayCommand _securityNoticeLearnMoreCommand;
        public ICommand SecurityNoticeLearnMoreCommand
        {
            get
            {
                return _securityNoticeLearnMoreCommand ??
                    (_securityNoticeLearnMoreCommand = new RelayCommand(unused =>
                    {
                        try
                        {
                            AppService.PageService.NavigateTo(typeof(WebBrowserPage), "https://go.microsoft.com/fwlink/?linkid=865702");
                        }
                        catch (Exception ex)
                        {
                            App.LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                        }
                    }));
            }
        }

        private RelayCommand _securityNoticeCloseCommand;
        public ICommand SecurityNoticeCloseCommand
        {
            get
            {
                return _securityNoticeCloseCommand ??
                    (_securityNoticeCloseCommand = new RelayCommand(unused =>
                    {
                        try
                        {
                            MakerImageBannerVisible = false;
                        }
                        catch (Exception ex)
                        {
                            App.LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                        }
                    }));
            }
        }
        #endregion
    }
    #region Helper Classes

    public class NetworkInfoDataTemplate
    {
        public string NetworkName { get; }
        public string NetworkIpv6 { get; }
        public string NetworkIpv4 { get; }
        public string NetworkStatus { get; }

        public NetworkInfoDataTemplate(NetworkPresenter.NetworkInfo network)
        {
            NetworkName = network.NetworkName;
            NetworkIpv6 = network.NetworkIpv6;
            NetworkIpv4 = network.NetworkIpv4;
            NetworkStatus = network.NetworkStatus;
        }
    }

    #endregion
}
