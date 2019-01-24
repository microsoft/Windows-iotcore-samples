// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Bluetooth;
using SmartDisplay.Contracts;
using SmartDisplay.Identity;
using SmartDisplay.Logging;
using SmartDisplay.Telemetry;
using SmartDisplay.Updates;
using SmartDisplay.Utils;
using SmartDisplay.Utils.UI;
using SmartDisplay.Views;
using SmartDisplay.Weather;
using System;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation.Diagnostics;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public sealed partial class App : Application, IAppServiceProvider
    {
        // Handler for Inbound pairing requests
        public static event InboundPairingRequestedHandler InboundPairingRequested;

        // Manages the settings for the app
        public static SettingsProvider Settings;

        // Authentication
        public static AuthenticationManager AuthManager;

        // Multi-View Manager
        public static MultiViewManager MultiViewManager;

        public static NetworkPresenter NetworkPresenter { get; } = new NetworkPresenter();

        // Logging service
        public static ILogService LogService { get; } = new EtwLogService(Constants.EtwProviderName, new Guid(Constants.EtwProviderGuid));

        // Telemetry service - can plug in one or more services
        public static ITelemetryService TelemetryService { get; private set; }

        // IAppServiceProvider
        public IAppService FindOrCreate(CoreDispatcher dispatcher) => AppService.FindOrCreate(dispatcher);
        public IAppService GetForCurrentContext() => AppService.GetForCurrentContext();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            // Load external components
            AppComposer.Initialize();

            Settings = SettingsProvider.GetDefault();
            Settings.SettingsUpdated += Settings_SettingsUpdated;
            AuthManager = new AuthenticationManager();

            // MultiTelemetryService uses AppComposer to import ITelemetryServices from external assemblies
            TelemetryService = new MultiTelemetryService
            {
                IsEnabled = Settings.AppEnableTelemetry
            };

            if (LogService is EtwLogService logService)
            {
                logService.SetTelemetryService(TelemetryService);
            }

            InitializeComponent();
            Suspending += OnSuspending;
            UnhandledException += App_UnhandledException;
            EnteredBackground += App_EnteredBackground;
            LeavingBackground += App_LeavingBackground;

            UpdateManager.StartAppUpdateChecker(TimeSpan.FromMinutes(10));
        }

        private ThreadPoolTimer _delayTimer;
        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (sender is SettingsProvider settings)
            {
                switch (args.Key)
                {
                    case "AppEnableTelemetry":
                        TelemetryService.IsEnabled = settings.AppEnableTelemetry;
                        break;
                }
            }

            // Only send one event if many updates come in at the same time
            _delayTimer?.Cancel();
            _delayTimer = ThreadPoolTimer.CreateTimer((t) =>
            {
                TelemetryService.WriteEvent("AppPreferencesSaved");
            }, TimeSpan.FromSeconds(1));
        }

        private void AzureIoTHub_HubInitialized(object sender, EventArgs e)
        {
            TelemetryService.WriteEvent("AzureIoTHubInitialized");
        }

        private void AzureIoTHub_DesiredPropertyUpdated(object sender, EventArgs e)
        {
            TelemetryService.WriteEvent("DesiredPropertyUpdated");
        }

        // Helper to check if device is connected to internet
        public static bool IsConnectedInternet()
        {
            ConnectionProfile connection = NetworkInformation.GetInternetConnectionProfile();
            return connection != null && connection.GetNetworkConnectivityLevel() == NetworkConnectivityLevel.InternetAccess;
        }

        private void App_LeavingBackground(object sender, LeavingBackgroundEventArgs e)
        {
            LogService.Write();
        }

        private void App_EnteredBackground(object sender, EnteredBackgroundEventArgs e)
        {
            LogService.Write();
        }

        private async void App_UnhandledException(object sender, Windows.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            try
            {
                LogService.WriteException(e.Exception);
                LogService.Write(e.Exception.ToString(), LoggingLevel.Critical);
                var file = await LogService.SaveLogToFileAsync("Crash_" + Constants.EtwProviderName);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user. Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            LogService.Write();

            // Check if this is the first time the app is being launched
            // If OnLaunched is called again when the app is still running (e.g. selecting "Switch to" in WDP
            // while the app is running), the initialization code should not run again
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                // Try to set the map token from file located in LocalState folder
                await WeatherHelper.SetMapTokenFromFileAsync("MapToken.config");

                // Enable kiosk mode if file is detected
                Settings.KioskMode = await FileUtil.ReadFileAsync("kiosk_mode") != null;

                // Use MEF to import features and components from other assemblies in the appx package.
                foreach (var feature in AppComposer.Imports.Features)
                {
                    try
                    {
                        LogService.Write($"Loading Feature: {feature.FeatureName}");
                        feature.OnLoaded(this);
                    }
                    catch (Exception ex)
                    {
                        LogService.Write(ex.ToString());
                    }
                }

                // Enable multi-view 
                MultiViewManager = new MultiViewManager(Window.Current.Dispatcher, ApplicationView.GetForCurrentView().Id);
                MultiViewManager.ViewAdded += MultiViewManager_ViewAdded;

                // Subscribe for IoT Hub property updates if available
                var iotHubService = GetForCurrentContext().GetRegisteredService<IIoTHubService>();
                if (iotHubService != null)
                {
                    iotHubService.DesiredPropertyUpdated += App_DesiredPropertyUpdated;
                }

                // Make sure we weren't launched in the background
                if (e != null && e.PrelaunchActivated == false)
                {
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter

#if !FORCE_OOBE_WELCOME_SCREEN
                    if (ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
                    {
                        rootFrame.Navigate(typeof(MainPage), e.Arguments);
                    }
                    else
#endif
                    {
                        rootFrame.Navigate(typeof(OOBEWelcomePage), e.Arguments);
                    }
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();

            // Update asynchronously
            var updateAsyncTask = NetworkPresenter.UpdateAvailableNetworksAsync(false);
        }

        /// <summary>
        /// Apply desired properties from IoT Hub device twin
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void App_DesiredPropertyUpdated(object sender, DesiredPropertyUpdatedEventArgs args)
        {
            LogService.Write();

            if (sender is IIoTHubService iotHubService)
            {
                var desiredSettings = args.DesiredProperties;

                if (desiredSettings == null)
                {
                    LogService.Write("Desired settings cannot be null");
                }

                desiredSettings.TryGetValue("$version", out object version);

                int settingsApplied = 0;

                // Apply any Smart Display settings
                foreach (var kvp in desiredSettings)
                {
                    try
                    {
                        string valueStr = IoTHubUtil.ConvertPropertyToString(desiredSettings[kvp.Key]);
                        if (Common.SetPropertyByName<SettingsProvider>(kvp.Key, Settings, valueStr))
                        {
                            LogService.Write($"Setting saved - Key: {kvp.Key}, Value: {valueStr}");

                            // Send acknowledgement asynchronously
                            var unused = iotHubService.AcknowledgeDesiredPropertyChangeAsync(kvp.Key, valueStr, version);

                            settingsApplied++;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.Write(ex.ToString());
                    }
                }

                if (settingsApplied > 0)
                {
                    GetForCurrentContext().PageService?.ShowNotification(string.Format(Common.GetLocalizedText("DeviceTwinUpdatedSettingsFormat"), settingsApplied), clickHandler: () =>
                    {
                        GetForCurrentContext().PageService?.NavigateTo(typeof(SettingsPage));
                    });
                }
            }
        }

        protected override void OnActivated(IActivatedEventArgs args)
        {
            LogService.Write();

            // Spot if we are being activated due to inbound pairing request
            if (args.Kind == ActivationKind.DevicePairing)
            {
                LogService.Write("ActivationKind.DevicePairing");

                // Get the arguments, which give information about the device which wants to pair with this app
                if (args is DevicePairingActivatedEventArgs devicePairingArgs)
                {
                    var deviceInfo = devicePairingArgs.DeviceInformation;

                    // Automatically switch to Bluetooth Settings page
                    var appService = AppService.FindOrCreate(Window.Current.Dispatcher);
                    if (appService.PageService != null)
                    {
                        appService.PageService.NavigateTo(typeof(SettingsPage), Common.GetLocalizedText("BluetoothPreferences/Text"));
                    }
                    else
                    {
                        Frame rootFrame = EnsureRootFrame();
                        rootFrame.Navigate(typeof(MainPage));
                    }

                    // Let subscribers know there's a new inbound request.
                    InboundPairingRequested?.Invoke(this, new InboundPairingEventArgs(deviceInfo));
                }
            }
            else
            {
                Frame rootFrame = EnsureRootFrame();
                rootFrame.Navigate(typeof(MainPage));
            }
        }

        private Frame EnsureRootFrame()
        {
            if (!(Window.Current.Content is Frame rootFrame))
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();
                rootFrame.NavigationFailed += OnNavigationFailed;

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            return rootFrame;
        }

        /// <summary>
        /// Invoked when Navigation to a certain page fails
        /// </summary>
        /// <param name="sender">The Frame which failed navigation</param>
        /// <param name="e">Details about the navigation failure</param>
        private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
        }

        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            LogService.Write();

            var deferral = e.SuspendingOperation.GetDeferral();

            deferral.Complete();
        }

        /// <summary>
        /// Invoked when a new secondary view is created.
        /// </summary>
        private void MultiViewManager_ViewAdded(object sender, ViewLifetimeControlEventArgs args)
        {
            args.ViewControl.Released += ViewControl_Released;
        }

        /// <summary>
        /// Invoked when a secondary view is destroyed.
        /// </summary>
        private void ViewControl_Released(object sender, EventArgs e)
        {
            if (sender is ViewLifetimeControl viewControl)
            {
                viewControl.Released -= ViewControl_Released;
                AppService.Remove(viewControl.Dispatcher);
            }
        }
    }
}
