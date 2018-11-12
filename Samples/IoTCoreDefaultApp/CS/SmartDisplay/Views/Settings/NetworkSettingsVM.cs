// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Views.Settings;
using System.Windows.Input;

namespace SmartDisplay.ViewModels.Settings
{
    public class NetworkSettingsVM : BaseViewModel
    {
        #region UI properties and commands

        public DirectConnectionControl DirectConnectionElement
        {
            get { return GetStoredProperty<DirectConnectionControl>(); }
            set { SetStoredProperty(value); }
        }

        public NetworkListControl NetworkListElement
        {
            get { return GetStoredProperty<NetworkListControl>(); }
            set { SetStoredProperty(value); }
        }

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }
        
        private RelayCommand _networkPropCommand;
        public ICommand NavigateNetworkPropertiesCommand
        {
            get
            {
                return _networkPropCommand ??
                    (_networkPropCommand = new RelayCommand(unused => PageService?.NavigateTo(typeof(NetworkPropertiesPage))));
            }
        }

        #endregion

        public NetworkSettingsVM() : base()
        {
            Width = Constants.SettingsWidth;

            // Override default subtitle font size for formatting consistency
            DirectConnectionElement = new DirectConnectionControl
            {
                SubtitleFontSize = 16
            };

            NetworkListElement = new NetworkListControl
            {
                SubtitleFontSize = 16
            };
        }

        public async void SetUpVM()
        {
            DirectConnectionElement.SetUpDirectConnection();
            await NetworkListElement.RefreshWiFiListViewItemsAsync(true);
        }

    }
}
