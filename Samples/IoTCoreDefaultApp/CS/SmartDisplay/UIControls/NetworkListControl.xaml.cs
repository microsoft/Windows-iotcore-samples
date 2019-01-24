// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.WiFi;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class NetworkListControl : UserControl
    {
        public event EventHandler<EventArgs> NetworkConnected;
        public ObservableCollection<WiFiListViewItemPresenter> WiFiListViewItems = new ObservableCollection<WiFiListViewItemPresenter>();

        private NetworkPresenter _networkPresenter = new NetworkPresenter();
        private string _currentPassword = string.Empty;
        private bool _automatic = true;
        private Frame _rootFrame;
        private SemaphoreSlim _availableNetworksLock = new SemaphoreSlim(1, 1);

        private int? _subtitleFontSize = null;
        public int SubtitleFontSize
        {
            get
            {
                if (_subtitleFontSize == null)
                {
                    _subtitleFontSize = 20;
                }

                return (int)_subtitleFontSize;
            }

            set
            {
                _subtitleFontSize = value;
            }
        }

        public NetworkListControl()
        {
            InitializeComponent();
            		
            DataContext = LanguageManager.GetInstance();
            WiFiListView.ItemsSource = WiFiListViewItems;

            _rootFrame = Window.Current.Content as Frame;
        }

        private void EnableView(bool enable, bool enableListView)
        {
            RefreshButton.IsEnabled = enable;
            RefreshButton.Visibility = enable ? Visibility.Visible : Visibility.Collapsed;
            RefreshProgressRing.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            WiFiListView.IsEnabled = enableListView;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshWiFiListViewItemsAsync(false);
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
        }

        public async Task RefreshWiFiListViewItemsAsync(bool refreshIfNeeded)
        {
            if (await _networkPresenter.WiFiIsAvailable())
            {
                bool isRefreshNeeded = _networkPresenter.IsRefreshNeeded();
                EnableView(false, false);

                try
                {
                    var networks = await _networkPresenter.GetAvailableNetworks(refreshIfNeeded);
                    if (networks.Count > 0)
                    {
                        var connectedNetwork = _networkPresenter.GetCurrentOOBENetworkName();
                        if (connectedNetwork != null)
                        {
                            networks.Remove(connectedNetwork);
                            networks.Insert(0, connectedNetwork);
                        }

                        WiFiListView.ItemsSource = WiFiListViewItems = new ObservableCollection<WiFiListViewItemPresenter>(networks);

                        var item = SwitchToItemState(connectedNetwork, WiFiConnectedState, true);
                        if (item != null)
                        {
                            WiFiListView.SelectedItem = item;
                        }

                        NoWiFiFoundText.Visibility = Visibility.Collapsed;
                        WiFiListView.Visibility = Visibility.Visible;

                        EnableView(true, true);

                        return;
                    }
                }
                catch (Exception e)
                {
                    App.LogService.Write(string.Format("Error scanning: 0x{0:X}: {1}", e.HResult, e.Message));
                    NoWiFiFoundText.Text = e.Message;
                    NoWiFiFoundText.Visibility = Visibility.Visible;
                    EnableView(true, true);
                    return;
                }
            }

            NoWiFiFoundText.Visibility = Visibility.Visible;
            WiFiListView.Visibility = Visibility.Collapsed;
        }

        private void WiFiListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var connectedNetwork = _networkPresenter.GetOOBECurrentWifiNetwork();
            var item = e.ClickedItem;
            if (connectedNetwork == item)
            {
                SwitchToItemState(item, WiFiConnectedMoreOptions, true);
            }
        }

        private void WiFiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var connectedNetwork = _networkPresenter.GetOOBECurrentWifiNetwork();

            foreach (var item in e.RemovedItems)
            {
                if (connectedNetwork == item)
                {
                    SwitchToItemState(item, WiFiConnectedState, true);
                }
                else
                {
                    SwitchToItemState(item, WiFiInitialState, true);
                }
            }

            foreach (var item in e.AddedItems)
            {
                _automatic = true;
                if (connectedNetwork == item)
                {
                    SwitchToItemState(connectedNetwork, WiFiConnectedMoreOptions, true);
                }
                else
                {
                    SwitchToItemState(item, WiFiConnectState, true);
                }
            }

            WiFiListView.ScrollIntoView(WiFiListView.SelectedItem);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);

                var button = sender as Button;
                var network = button.DataContext as WiFiListViewItemPresenter;
                if (NetworkPresenter.IsNetworkOpen(network))
                {
                    await ConnectToWiFiAsync(network, null, Window.Current.Dispatcher);
                }
                else if (network.IsEapAvailable)
                {
                    SwitchToItemState(network, WiFiEapPasswordState, false);
                }
                else
                {
                    SwitchToItemState(network, WiFiPasswordState, false);
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }

        private async Task OnConnected(WiFiListViewItemPresenter network, WiFiConnectionStatus status, CoreDispatcher dispatcher)
        {
            if (status == WiFiConnectionStatus.Success)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var itemLocation = WiFiListViewItems.IndexOf(network);

                    // Don't move if index is -1 or 0
                    if (itemLocation > 0)
                    {
                        // Make sure first network doesn't also show connected
                        SwitchToItemState(WiFiListViewItems[0], WiFiInitialState, true);

                        // Show current connected network at top of list in connected state
                        WiFiListViewItems.Move(itemLocation, 0);
                    }

                    network.Message = string.Empty;
                    network.IsMessageVisible = false;

                    var item = SwitchToItemState(network, WiFiConnectedState, true);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                });

                if (!NetworkPresenter.IsNetworkOpen(network))
                {
                    NetworkConnected?.Invoke(this, new EventArgs());
                }
            }
            else
            {
                // Entering the wrong password may cause connection attempts to timeout
                // Disconnecting the adapter will return it to a non-busy state
                if (status == WiFiConnectionStatus.Timeout)
                {
                    network.Adapter.Disconnect();
                }
                var resourceLoader = ResourceLoader.GetForCurrentView();
                network.Message = Common.GetLocalizedText(status.ToString() + "Text");
                network.IsMessageVisible = true;
                SwitchToItemState(network, WiFiConnectState, true);
            }
        }

        public async Task<WiFiConnectionStatus> ConnectToNetwork(WiFiListViewItemPresenter network, bool autoConnect)
        {
            await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                _availableNetworksLock.WaitAsync();
            });

            if (network == null)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }

            try
            {
                var result = await network.Adapter.ConnectAsync(network.AvailableNetwork, autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual);

                // Call redirect only for Open WiFi
                if (NetworkPresenter.IsNetworkOpen(network))
                {
                    // Navigate to http://www.msftconnecttest.com/redirect 
                    await Window.Current.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                    {
                        await DoRedirectDialogAsync();
                    });
                }

                App.LogService.Write($"LEAVE {result.ConnectionStatus}");
                return result.ConnectionStatus;
            }
            catch (Exception)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }
        }

        private async Task<bool> DoRedirectDialogAsync()
        {
            AppService appService = AppService.GetForCurrentContext() as AppService;
            var title = Common.GetLocalizedText("NetworkRedirectTitle");
            var message = Common.GetLocalizedText("NetworkRedirectText");

            DialogButton primaryButton = new DialogButton(Common.GetLocalizedText("ContinueButton/Content"), (Sender, ClickEventsArgs) =>
            {
                if (appService.PageService != null)
                {
                    appService.PageService.NavigateTo(typeof(WebBrowserPage), UrlConstants.MicrosoftWiFiConnectURL);
                }
                else
                {
                    _rootFrame.Navigate(typeof(MainPage), Tuple.Create(typeof(WebBrowserPage), UrlConstants.MicrosoftWiFiConnectURL));
                }
            });

            DialogButton closeButton = new DialogButton(Common.GetLocalizedText("CancelButton/Content"), (Sender, ClickEventsArgs) => {});

            if (appService.PageService != null)
            {
                await appService.DisplayDialogAsync(title, message, primaryButton, null, closeButton);
            }
            else
            {
                var currentDialog = new ContentDialog()
                {
                    Title = title,
                    Content = message,
                    PrimaryButtonText = primaryButton.Name,
                    CloseButtonText = closeButton.Name,
                };

                currentDialog.PrimaryButtonClick += primaryButton.ClickEventHandler;
                currentDialog.CloseButtonClick += closeButton.ClickEventHandler;

                await currentDialog.ShowAsync().AsTask();
            }

            return true;
        }

        public async Task<WiFiConnectionStatus> ConnectToNetworkWithPassword(WiFiListViewItemPresenter network, bool autoConnect, PasswordCredential password)
        {
            try
            {
                var result = await network.Adapter.ConnectAsync(
                    network.AvailableNetwork,
                    autoConnect ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual,
                    password);

                App.LogService.Write($"LEAVE {result.ConnectionStatus}");
                return result.ConnectionStatus;
            }
            catch (Exception)
            {
                return WiFiConnectionStatus.UnspecifiedFailure;
            }
        }

        private async Task ConnectToWiFiAsync(WiFiListViewItemPresenter network, PasswordCredential credential, CoreDispatcher dispatcher)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SwitchToItemState(network, WiFiConnectingState, false);
            });

            Task<WiFiConnectionStatus> didConnect = null;
            if (network.IsEapAvailable)
            {
                didConnect = (credential == null) ?
                    ConnectToNetwork(network, _automatic) :
                    ConnectToNetworkWithPassword(network, _automatic, credential);
            }
            else
            {
                didConnect = (credential == null) ?
                    ConnectToNetwork(network, network.ConnectAutomatically) :
                    ConnectToNetworkWithPassword(network, network.ConnectAutomatically, credential);
            }

            WiFiConnectionStatus status = await didConnect;
            await OnConnected(network, status, dispatcher);
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var network = button.DataContext as WiFiListViewItemPresenter;
                var connectedNetwork = _networkPresenter.GetCurrentOOBENetworkName();

                if (network == connectedNetwork)
                {
                    _networkPresenter.DisconnectNetwork(network);
                    var item = SwitchToItemState(network, WiFiInitialState, true);
                    item.IsSelected = false;
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
        }

        private async void NextButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);
                var button = sender as Button;
                PasswordCredential credential;
                if (button.DataContext is WiFiListViewItemPresenter network)
                {
                    if (string.IsNullOrEmpty(network.Password) && string.IsNullOrEmpty(_currentPassword))
                    {
                        credential = null;
                    }
                    else if (!string.IsNullOrEmpty(_currentPassword))
                    {
                        credential = new PasswordCredential()
                        {
                            Password = _currentPassword
                        };
                    }
                    else
                    {
                        credential = new PasswordCredential();
                        if (network.UsePassword)
                        {
                            if (!string.IsNullOrEmpty(network.Domain))
                            {
                                credential.Resource = network.Domain;
                            }
                            credential.UserName = network.UserName ?? string.Empty;
                            credential.Password = network.Password ?? string.Empty;
                        }
                        else
                        {
                            credential.Password = network.Password;
                        }
                    }

                    await ConnectToWiFiAsync(network, credential, Window.Current.Dispatcher);
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cancels the UI state but not the connection attempt
                var button = sender as Button;
                var item = SwitchToItemState(button.DataContext, WiFiConnectState, false);
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
        }

        private ListViewItem SwitchToItemState(object dataContext, DataTemplate template, bool forceUpdate)
        {
            if (WiFiConnectedState.Equals(template))
            {
                ServiceUtil.TelemetryService.WriteEvent("WiFiConnectSuccess");
            }

            if (forceUpdate)
            {
                WiFiListView.UpdateLayout();
            }
            var item = WiFiListView.ContainerFromItem(dataContext) as ListViewItem;
            if (item != null)
            {
                item.ContentTemplate = template;
            }

            return item;
        }

        private void WiFiPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox)
            {
                passwordBox.Focus(FocusState.Programmatic);
            }
        }

        private void ConnectAutomaticallyCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var checkbox = sender as CheckBox;

            _automatic = checkbox.IsChecked ?? false;
        }

        private void WiFiPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            _currentPassword = passwordBox.Password;
        }

        private async void PushButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);
                var button = sender as Button;
                if (button.DataContext is WiFiListViewItemPresenter network)
                {
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5, 0))
                    {
                        var didConnect = await network.Adapter.ConnectAsync(
                            network.AvailableNetwork,
                            network.ConnectAutomatically ? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual,
                            null,
                            string.Empty,
                            WiFiConnectionMethod.WpsPushButton).AsTask();
                        await OnConnected(network, didConnect.ConnectionStatus, Window.Current.Dispatcher);
                    }
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }
    }
}
