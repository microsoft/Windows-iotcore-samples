// Copyright (c) Microsoft. All rights reserved.

using IoTCoreDefaultApp.Utils;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

namespace IoTCoreDefaultApp
{
    public sealed partial class MainPage : Page
    {
        public static MainPage Current;
        private CoreDispatcher MainPageDispatcher;
        private ConnectedDevicePresenter connectedDevicePresenter;
        private const string CommandLineProcesserExe = "c:\\windows\\system32\\cmd.exe";
        private const string RegKeyQueryCmdArg = "/c \"reg query HKEY_LOCAL_MACHINE\\Software\\Microsoft\\Windows\\IoT /v IsMakerImage /z\"";
        private const string ExpectedResultPattern = @"\s*IsMakerImage\s*REG_DWORD\s*\(4\)\s*0x1";
        private const uint CmdLineBufSize = 8192;

        public CoreDispatcher UIThreadDispatcher
        {
            get
            {
                return MainPageDispatcher;
            }

            set
            {
                MainPageDispatcher = value;
            }
        }

        public MainPage()
        {
            Log.Enter();
            this.InitializeComponent();

            // This is a static public property that allows downstream pages to get a handle to the MainPage instance
            // in order to call methods that are in this class.
            Current = this;

            MainPageDispatcher = Window.Current.Dispatcher;

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            this.NavigationCacheMode = NavigationCacheMode.Enabled;

            this.DataContext = LanguageManager.GetInstance();

            UpdateMakerImageSecurityNotice();

            this.Loaded += async (sender, e) =>
            {
                Log.Enter("MainPage Loaded");
                await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    UpdateBoardInfo();
                    UpdateNetworkInfo();
                    UpdateConnectedDevices();
                    UpdatePackageVersion();
                });
                Log.Leave();
            };
            Log.Leave();
        }

        private async void UpdateMakerImageSecurityNotice()
        {
            if (await IsMakerImager())
            {
                await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    SecurityNoticeRow.Visibility = Visibility.Visible;
                });
            }
        }

        private async Task<bool> IsMakerImager()
        {
            var cmdOutput = string.Empty;

            var standardOutput = new InMemoryRandomAccessStream();
            var options = new ProcessLauncherOptions
            {
                StandardOutput = standardOutput
            };

            try
            {
                var result = await ProcessLauncher.RunToCompletionAsync(CommandLineProcesserExe, RegKeyQueryCmdArg, options);

                if (result.ExitCode == 0)
                {
                    using (var outStreamRedirect = standardOutput.GetInputStreamAt(0))
                    {
                        using (var dataReader = new DataReader(outStreamRedirect))
                        {
                            uint bytesLoaded = 0;
                            while ((bytesLoaded = await dataReader.LoadAsync(CmdLineBufSize)) > 0)
                            {
                                cmdOutput += dataReader.ReadString(bytesLoaded);
                            }
                        }
                    }
                }

                Match match = Regex.Match(cmdOutput, ExpectedResultPattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                // Could not read the value
                Log.Write("Could not read maker image value in registry");
                Log.Write(ex.ToString());
            }

            return false;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
            {
                ApplicationData.Current.LocalSettings.Values[Constants.HasDoneOOBEKey] = Constants.HasDoneOOBEValue;
            }

            base.OnNavigatedTo(e);
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            await MainPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateNetworkInfo();
            });
        }

        private void UpdateBoardInfo()
        {
            BoardName.Text = DeviceInfoPresenter.GetBoardName();
            BoardImage.Source = new BitmapImage(DeviceInfoPresenter.GetBoardImageUri());

            ulong version = 0;
            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out version))
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                OSVersion.Text = loader.GetString("OSVersionNotAvailable");
            }
            else
            {
                OSVersion.Text = String.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    version & 0x000000000000FFFF);
            }
        }

        private void UpdatePackageVersion()
        {
            AppxVersion.Text = String.Format(CultureInfo.InvariantCulture, "v{0}.{1}.{2}.{3}",
              Package.Current.Id.Version.Major,
              Package.Current.Id.Version.Minor,
              Package.Current.Id.Version.Build,
              Package.Current.Id.Version.Revision);
        }

        private void WindowsOnDevices_Click(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(WebBrowserPage), Constants.WODUrl);
        }

        private async void UpdateNetworkInfo()
        {
            this.DeviceName.Text = DeviceInfoPresenter.GetDeviceName();
            this.IPAddress1.Text = NetworkPresenter.GetCurrentIpv4Address();
            this.NetworkName1.Text = NetworkPresenter.GetCurrentNetworkName();
            this.NetworkInfo.ItemsSource = await NetworkPresenter.GetNetworkInformation();
        }

        private void UpdateConnectedDevices()
        {
            connectedDevicePresenter = new ConnectedDevicePresenter(MainPageDispatcher);
            this.ConnectedDevices.ItemsSource = connectedDevicePresenter.GetConnectedDevices();
        }

        private void CloseNoticeButton_Click(object sender, RoutedEventArgs e)
        {
            SecurityNoticeRow.Visibility = Visibility.Collapsed;
        }

        private void SecurityNoticeLearnMoreButton_Click(object sender, RoutedEventArgs e)
        {
            NavigationUtils.NavigateToScreen(typeof(WebBrowserPage), Constants.IoTCoreManufacturingGuideUrl);
        }
    }
}
