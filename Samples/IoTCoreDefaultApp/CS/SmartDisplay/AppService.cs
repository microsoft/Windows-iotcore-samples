// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Bluetooth;
using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Identity;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay
{
    public class AppService : IAppService
    {
        private static Dictionary<CoreDispatcher, AppService> s_appServices = new Dictionary<CoreDispatcher, AppService>();
        private static object s_appServicesLock = new object();

        // NullDispatcherService provides access to non-UI services when there is no current window.
        private static AppService s_nullDispatcherService;
        private static AppService NullDispatcherService
        {
            get
            {
                if (s_nullDispatcherService == null)
                {
                    s_nullDispatcherService = new AppService(null);
                }
                return s_nullDispatcherService;
            }
        }

        public static IAppService FindOrCreate(CoreDispatcher dispatcher)
        {
            if (dispatcher == null)
            {
                return NullDispatcherService;
            }

            lock (s_appServicesLock)
            {
                if (!s_appServices.TryGetValue(dispatcher, out var service))
                {
                    service = new AppService(dispatcher);
                    s_appServices[dispatcher] = service;
                }
                return service;
            }
        }

        public static IAppService GetForCurrentContext()
        {
            return FindOrCreate(Window.Current?.Dispatcher);
        }

        public static void Remove(CoreDispatcher dispatcher)
        {
            lock (s_appServicesLock)
            {
                s_appServices.Remove(dispatcher);
            }
        }

        private UiManager _uiManager;
        private CoreDispatcher Dispatcher { get; set; }

        private AppService(CoreDispatcher dispatcher)
        {
            Dispatcher = dispatcher;
        }

        /// <summary>
        /// Sets the service to display UI elements.
        /// </summary>
        /// <param name="service"></param>
        public void SetPageService(IPageService service)
        {
            if (PageService != service)
            {
                if (PageService != null)
                {
                    UnsubscribeToEvents();
                    PageService = null;
                    _uiManager = null;
                }

                if (service != null)
                {
                    PageService = service;
                    _uiManager = UiManager.Find(Dispatcher);
                    SubscribeToEvents();
                }
            }
        }

        #region IAppService

        public IPageService PageService { get; private set; }

        public ILogService LogService => App.LogService;

        public ITelemetryService TelemetryService => App.TelemetryService;

        public IAuthManager AuthManager => App.AuthManager;

        public ISettingsProvider Settings => App.Settings;

        public Type[] RegisteredPages => PageUtil.GetAllPageTypes();

        public bool IsConnectedInternet() => App.IsConnectedInternet();

        public Task RunExclusiveTaskAsync(Action task) => _uiManager?.RunExclusiveTaskAsync(task);

        public Task RunExclusiveTaskAsync(Func<Task> task) => _uiManager?.RunExclusiveTaskAsync(task);

        public Task<T> RunExclusiveTaskAsync<T>(Func<Task<T>> task) => _uiManager?.RunExclusiveTaskAsync<T>(task);

        /// <summary>
        /// Creates a Yes/No dialog
        /// </summary>
        /// <param name="title">Dialog title</param>
        /// <param name="question">Dialog question</param>
        /// <param name="primaryButtonText">Text that appears on the "yes" button</param>
        /// <param name="secondaryButtonText">Text that appears on the "no" button</param>
        public async Task<bool> YesNoAsync(
            string title,
            string question,
            string primaryButtonText = null,
            string secondaryButtonText = null)
        {
            if (_uiManager == null)
            {
                return false;
            }

            if (string.IsNullOrWhiteSpace(primaryButtonText))
            {
                primaryButtonText = Common.GetLocalizedText("YesButton/Content");
            }

            if (string.IsNullOrWhiteSpace(secondaryButtonText))
            {
                secondaryButtonText = Common.GetLocalizedText("NoButton/Content");
            }

            var tcs = new TaskCompletionSource<bool>();

            await Dispatcher?.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                UserDialog dlg = new UserDialog(title, question, primaryButtonText, secondaryButtonText);
                ContentDialogResult dialogResult = await _uiManager?.RunExclusiveTaskAsync(() => dlg.ShowAsync().AsTask());
                tcs.SetResult(dlg.Result);
            });

            bool yesNo = await tcs.Task;

            return yesNo;
        }

        /// <summary>
        /// Displays a ContentDialog with 1 - 3 customizable buttons. 
        /// Returns before dialog is dismissed.
        /// </summary>
        /// <param name="title"></param>
        /// <param name="content"></param>
        /// <param name="primaryButton">Will show up as the leftmost button. Set to null if not needed.</param>
        /// <param name="secondaryButton">Will show up as the center button if primary is set. Set to null if not needed.</param>
        /// <param name="closeButton">Always enabled and "Close" is the default text</param>
        public void DisplayDialog(string title, object content, DialogButton? primaryButton, DialogButton? secondaryButton, DialogButton? closeButton)
        {
            var noWait = _uiManager?.DisplayDialogAsync(title, content, primaryButton, secondaryButton, closeButton);
        }

        public Task DisplayDialogAsync(string title, object content, DialogButton? primaryButton, DialogButton? secondaryButton, DialogButton? closeButton)
        {
            return _uiManager?.DisplayDialogAsync(title, content, primaryButton, secondaryButton, closeButton);
        }

        /// <summary>
        /// Creates dialog to notify user that there is no internet
        /// </summary>
        /// <param name="redirectPage">Page to redirect to if 'Close' is selected, null if no redirect needed</param>
        public void DisplayNoInternetDialog(Type redirectPage = null)
        {
            DialogButton primaryButton = new DialogButton(Common.GetLocalizedText("DeviceInfoButton"), (Sender, ClickEventsArgs) =>
            {
                PageService?.NavigateTo(typeof(DeviceInfoPage));
            });

            DialogButton secondaryButton = new DialogButton(Common.GetLocalizedText("WifiSettingsButton"), (Sender, ClickEventsArgs) =>
            {
                PageService?.NavigateTo(typeof(SettingsPage), Common.GetLocalizedText("NetworkPreferences/Text"));
            });

            DialogButton closeButton = new DialogButton(Common.GetLocalizedText("CloseButton"), (Sender, ClickEventsArgs) =>
            {
                if (redirectPage != null)
                {
                    PageService?.NavigateTo(redirectPage);
                }
            });

            DisplayDialog(Common.GetLocalizedText("NoInternetConnectionText"), Common.GetLocalizedText("CheckConnectionText"), primaryButton, secondaryButton, closeButton);
        }

        /// <summary>
        /// All prompts for Graph sign in should go through this dialog. Always check to see if signed in 
        /// before using Graph API and redirect to this dialog if sign in is needed.
        /// </summary>
        /// <param name="returnPage"></param>
        public void DisplayAadSignInDialog(Type returnPage, string title = null)
        {
            if (string.IsNullOrEmpty(title))
            {
                title = Common.GetLocalizedText("AuthAadSignInTitle");
            }

            DialogButton primaryButton = new DialogButton(Common.GetLocalizedText("AuthSignInText"), (Sender, ClickEventsArgs) =>
            {
                PageService?.NavigateTo(typeof(AuthenticationPage), reload: true);
            });

            DialogButton closeButton = new DialogButton(Common.GetLocalizedText("CloseText"), (Sender, ClickEventsArgs) =>
            {
                PageService?.NavigateTo(typeof(TilePage), reload: true);
            });

            DisplayDialog(title, Common.GetLocalizedText("SignInAADMessage"), primaryButton, null, closeButton);
        }

        private static Dictionary<Type, ISmartDisplayService> _registeredServices = new Dictionary<Type, ISmartDisplayService>();
        private static object _regLock = new object();

        /// <summary>
        /// Registers service with the AppService
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <returns>True if successful, false otherwise</returns>
        public bool RegisterService<T>(T service) where T : ISmartDisplayService
        {
            lock (_regLock)
            {
                LogService.Write($"Registering service: {typeof(T).Name}...");

                if (!_registeredServices.ContainsKey(typeof(T)))
                {
                    _registeredServices.Add(typeof(T), service);
                    return true;
                }

                return false;
            }
        }

        /// <summary>
        /// Unregisters a service from the AppService
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="service"></param>
        /// <returns>True if successful, false otherwise</returns>
        public bool UnregisterService<T>(T service) where T : ISmartDisplayService
        {
            lock (_regLock)
            {
                LogService.Write($"Unregistering service: {typeof(T).Name}...");

                if (_registeredServices.TryGetValue(typeof(T), out var registered) && registered.Equals(service))
                {
                    return _registeredServices.Remove(typeof(T));
                }

                return false;
            }
        }

        /// <summary>
        /// Gets a previously registered service
        /// </summary>
        /// <typeparam name="T"></typeparam>
        public T GetRegisteredService<T>() where T : ISmartDisplayService
        {
            lock (_regLock)
            {
                if (_registeredServices.TryGetValue(typeof(T), out var registered))
                {
                    return (T)registered;
                }

                return default(T);
            }
        }

        #endregion

        #region Event handlers

        private void SubscribeToEvents()
        {
            // Subscribe to BT message events
            BluetoothServerHelper.Instance.MessageReceived += Bluetooth_MessageReceived;

            // Event handler for detecting when token status changes over time
            App.AuthManager.TokenStatusChanged += AuthManager_TokenStatusChanged;
        }

        private void UnsubscribeToEvents()
        {
            BluetoothServerHelper.Instance.MessageReceived -= Bluetooth_MessageReceived;
            App.AuthManager.TokenStatusChanged -= AuthManager_TokenStatusChanged;
        }

        private void Bluetooth_MessageReceived(object sender, BluetoothMessageReceivedArgs args)
        {
            PageService?.ShowNotification(args.Message, 3);
        }

        private async void AuthManager_TokenStatusChanged(object sender, TokenStatusEventArgs args)
        {
            App.LogService.Write($"Provider: {args.ProviderKey}, Old: {args.OldValue}, New: {args.NewValue}");

            var msa = AuthManager.GetProvider(ProviderNames.MsaProviderKey);
            bool isMsaValid = msa.IsTokenValid();

            LiveUserInfo userInfo = null;
            if (isMsaValid)
            {
                userInfo = await LiveApiUtil.GetUserInfoAsync(await AuthManager.GetProvider(ProviderNames.MsaProviderKey).GetTokenSilentAsync());
            }

            var aad = AuthManager.GetGraphProvider();
            bool isAadValid = (aad != null) ? aad.IsTokenValid() : false;

            // Update the UI
            PageService?.SetSignInStatus(isMsaValid, isAadValid, userInfo?.name);
        }

        #endregion
    }
}
