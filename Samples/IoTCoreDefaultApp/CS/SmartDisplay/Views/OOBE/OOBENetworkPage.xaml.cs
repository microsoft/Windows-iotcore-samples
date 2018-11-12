// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System;
using System.Windows.Input;
using Windows.Networking.Connectivity;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class OOBENetworkPage : OOBEPageBase
    {
        private CoreDispatcher _oobeNetworkPageDispatcher;
        private bool _connected = false;
        private Frame _rootFrame;

        private RelayCommand _nextButtonCommand;
        public ICommand NextButtonCommand
        {
            get
            {
                return _nextButtonCommand ??
                    (_nextButtonCommand = new RelayCommand(unused =>
                    {
                        _rootFrame.Navigate(typeof(MainPage));
                    }));
            }
        }

        public OOBENetworkPage()
        {
            InitializeComponent();
            _oobeNetworkPageDispatcher = Window.Current.Dispatcher;

            NetworkInformation.NetworkStatusChanged += NetworkInformation_NetworkStatusChanged;

            _rootFrame = Window.Current.Content as Frame;

            NavigationCacheMode = NavigationCacheMode.Enabled;

            DataContext = LanguageManager.GetInstance();

            Loaded += async (sender, e) =>
            {
                await _oobeNetworkPageDispatcher.RunAsync(CoreDispatcherPriority.Low, async () => {
                    DirectConnectControl.SetUpDirectConnection();
                    await NetworkControl.RefreshWiFiListViewItemsAsync(true);
                });
            };
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            NetworkInformation.NetworkStatusChanged -= NetworkInformation_NetworkStatusChanged;
        }

        private async void NetworkInformation_NetworkStatusChanged(object sender)
        {
            if (!_connected)
            {
                await _oobeNetworkPageDispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    DirectConnectControl.SetUpDirectConnection();
                });
            }
        }
    }
}
