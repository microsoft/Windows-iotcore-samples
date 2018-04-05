using IoTCoreDefaultApp.Presenters;
using IoTCoreDefaultApp.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.WiFi;
using Windows.Foundation.Metadata;
using Windows.Security.Credentials;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at https://go.microsoft.com/fwlink/?LinkId=234236

namespace IoTCoreDefaultApp
{
    public sealed partial class NetworkListControl : UserControl
    {
        public event EventHandler<EventArgs> NetworkConnected;

        public ObservableCollection<WifiListViewItemPresenter> WifiListViewItems = new ObservableCollection<WifiListViewItemPresenter>();
        
        public NetworkListControl()
        {
            this.InitializeComponent();

            this.DataContext = LanguageManager.GetInstance();
            WifiListView.ItemsSource = WifiListViewItems;
        }

        private void EnableView(bool enable, bool enableListView)
        {
            RefreshButton.IsEnabled = enable;
            RefreshButton.Visibility = enable? Visibility.Visible : Visibility.Collapsed;
            RefreshProgressRing.Visibility = enable ? Visibility.Collapsed : Visibility.Visible;

            WifiListView.IsEnabled = enableListView;
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RefreshWifiListViewItemsAsync(false);
            }
            catch (Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
        }

        public void SetupDirectConnection()
        {
            var ethernetProfile = NetworkPresenter.GetDirectConnectionName();

            if (ethernetProfile == null)
            {
                NoneFoundText.Visibility = Visibility.Visible;
                DirectConnectionStackPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoneFoundText.Visibility = Visibility.Collapsed;
                DirectConnectionStackPanel.Visibility = Visibility.Visible;
            }
        }

        public async Task RefreshWifiListViewItemsAsync(bool refreshIfNeeded)
        {
            if (await App.NetworkPresenter.WifiIsAvailable())
            {
                bool isRefreshNeeded = App.NetworkPresenter.IsRefreshNeeded();
                EnableView(false, false);

                try
                {
                    var networks = await App.NetworkPresenter.GetAvailableNetworks(refreshIfNeeded);
                    if (networks.Count > 0)
                    {
                        var connectedNetwork = App.NetworkPresenter.GetCurrentWifiNetwork();
                        if (connectedNetwork != null)
                        {
                            networks.Remove(connectedNetwork);
                            networks.Insert(0, connectedNetwork);
                        }

                        WifiListView.ItemsSource = WifiListViewItems = new ObservableCollection<WifiListViewItemPresenter>(networks);

                        var item = SwitchToItemState(connectedNetwork, WifiConnectedState, true);
                        if (item != null)
                        {
                            WifiListView.SelectedItem = item;
                        }

                        NoWifiFoundText.Visibility = Visibility.Collapsed;
                        WifiListView.Visibility = Visibility.Visible;

                        EnableView(true, true);

                        return;
                    }
                }
                catch (Exception e)
                {
                    Log.Write(String.Format("Error scanning: 0x{0:X}: {1}", e.HResult, e.Message));
                    NoWifiFoundText.Text = e.Message;
                    NoWifiFoundText.Visibility = Visibility.Visible;
                    EnableView(true, true);
                    return;
                }
            }

            NoWifiFoundText.Visibility = Visibility.Visible;
            WifiListView.Visibility = Visibility.Collapsed;
        }

        private void WifiListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var connectedNetwork = App.NetworkPresenter.GetCurrentWifiNetwork();
            var item = e.ClickedItem;
            if (connectedNetwork == item)
            {
                SwitchToItemState(item, WifiConnectedMoreOptions, true);
            }
        }

        private void WifiListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listView = sender as ListView;
            var connectedNetwork = App.NetworkPresenter.GetCurrentWifiNetwork();

            foreach (var item in e.RemovedItems)
            {
                if (connectedNetwork == item)
                {
                    SwitchToItemState(item, WifiConnectedState, true);
                }
                else
                {
                    SwitchToItemState(item, WifiInitialState, true);
                }
            }

            foreach (var item in e.AddedItems)
            {
                if (connectedNetwork == item)
                {
                    SwitchToItemState(connectedNetwork, WifiConnectedMoreOptions, true);
                }
                else
                {
                    SwitchToItemState(item, WifiConnectState, true);
                }
            }

            WifiListView.ScrollIntoView(WifiListView.SelectedItem);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);

                var button = sender as Button;
                var network = button.DataContext as WifiListViewItemPresenter;
                if (NetworkPresenter.IsNetworkOpen(network))
                {
                    await ConnectToWifiAsync(network, null, Window.Current.Dispatcher);
                }
                else if (network.IsEapAvailable)
                {
                    SwitchToItemState(network, WifiEapPasswordState, false);
                }
                else
                {
                    SwitchToItemState(network, WifiPasswordState, false);
                }
            }
            catch(Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }

        private async Task OnConnected(WifiListViewItemPresenter network, WiFiConnectionStatus status, CoreDispatcher dispatcher)
        {
            if (status == WiFiConnectionStatus.Success)
            {
                await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var itemLocation = WifiListViewItems.IndexOf(network);

                    // don't move if index is -1 or 0
                    if (itemLocation > 0)
                    {
                        // make sure first network doesn't also show connected
                        SwitchToItemState(WifiListViewItems[0], WifiInitialState, true);

                        // Show current connected network at top of list in connected state
                        WifiListViewItems.Move(itemLocation, 0);
                    }

                    network.Message = String.Empty;
                    network.IsMessageVisible = false;

                    var item = SwitchToItemState(network, WifiConnectedState, true);
                    if (item != null)
                    {
                        item.IsSelected = true;
                    }
                });

                NetworkConnected?.Invoke(this, new EventArgs());
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
                network.Message = resourceLoader.GetString(status.ToString() + "Text");
                network.IsMessageVisible = true;
                SwitchToItemState(network, WifiConnectState, true);
            }
        }

        private async Task ConnectToWifiAsync(WifiListViewItemPresenter network, PasswordCredential credential, CoreDispatcher dispatcher)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                SwitchToItemState(network, WifiConnectingState, false);
            });

            Task<WiFiConnectionStatus> didConnect = null;
            if (network.IsEapAvailable)
            {
                didConnect = (credential == null) ?
                    App.NetworkPresenter.ConnectToNetwork(network, network.ConnectAutomatically) :
                    App.NetworkPresenter.ConnectToNetworkWithPassword(network, network.ConnectAutomatically, credential);
            }
            else
            { 
                didConnect = (credential == null) ?
                    App.NetworkPresenter.ConnectToNetwork(network, network.ConnectAutomatically) :
                    App.NetworkPresenter.ConnectToNetworkWithPassword(network, network.ConnectAutomatically, credential);
            }

            WiFiConnectionStatus status = await didConnect;
            await OnConnected(network, status, dispatcher);
        }

        private void DisconnectButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                var button = sender as Button;
                var network = button.DataContext as WifiListViewItemPresenter;
                var connectedNetwork = App.NetworkPresenter.GetCurrentWifiNetwork();

                if (network == connectedNetwork)
                {
                    App.NetworkPresenter.DisconnectNetwork(network);
                    var item = SwitchToItemState(network, WifiInitialState, true);
                    item.IsSelected = false;
                }
            }
            catch (Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
        }

        private async void NextButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);
                var button = sender as Button;
                PasswordCredential credential;
                var network = button.DataContext as WifiListViewItemPresenter;
                if (network != null)
                {
                    if (string.IsNullOrEmpty(network.Password))
                    {
                        credential = null;
                    }
                    else
                    {
                        credential = new PasswordCredential();
                        if (network.UsePassword)
                        {
                            if (!String.IsNullOrEmpty(network.Domain))
                            {
                                credential.Resource = network.Domain;
                            }
                            credential.UserName = network.UserName ?? "";
                            credential.Password = network.Password ?? "";
                        }
                        else
                        {
                            credential.Password = network.Password;
                        }
                    }

                    await ConnectToWifiAsync(network, credential, Window.Current.Dispatcher);
                }
            }
            catch (Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }

        private void CancelButton_Clicked(object sender, RoutedEventArgs e)
        {
            try
            {
                // Cancels the UI state but not the connection attempt
                var button = sender as Button;
                var item = SwitchToItemState(button.DataContext, WifiConnectState, false);
            }
            catch (Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
        }

        private ListViewItem SwitchToItemState(object dataContext, DataTemplate template, bool forceUpdate)
        {
            if (forceUpdate)
            {
                WifiListView.UpdateLayout();
            }
            var item = WifiListView.ContainerFromItem(dataContext) as ListViewItem;
            if (item != null)
            {
                item.ContentTemplate = template;
            }

            return item;
        }

        private void WifiPasswordBox_Loaded(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.Focus(FocusState.Programmatic);
            }
        }

        private async void PushButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                EnableView(false, true);
                var button = sender as Button;
                var network = button.DataContext as WifiListViewItemPresenter;
                if (network != null)
                {
                    if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 5, 0))
                    {

                        var didConnect = await network.Adapter.ConnectAsync(network.AvailableNetwork, network.ConnectAutomatically? WiFiReconnectionKind.Automatic : WiFiReconnectionKind.Manual, null, String.Empty, WiFiConnectionMethod.WpsPushButton).AsTask<WiFiConnectionResult>();
                        await OnConnected(network, didConnect.ConnectionStatus, Window.Current.Dispatcher);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Trace(ex.ToString());
                throw;
            }
            finally
            {
                EnableView(true, true);
            }
        }
    }
}
