// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.Views;
using System.Windows.Input;

namespace SmartDisplay.ViewModels.Settings
{
    public class LocationSettingsVM : BaseViewModel
    {
        public bool IsLocationEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    if (Settings.IsLocationEnabled != value)
                    {
                        Settings.IsLocationEnabled = value;
                    }
                }
            }
        }

        private RelayCommand _hyperlinkCommand;
        public ICommand HyperlinkCommand
        {
            get
            {
                return _hyperlinkCommand ??
                    (_hyperlinkCommand = new RelayCommand((object parameter) =>
                    {
                        if (parameter is string hyperlinkType)
                        {
                            switch (hyperlinkType)
                            {
                                case "PrivacyStatement":
                                    PageService?.NavigateTo(typeof(WebBrowserPage), Constants.LocationPrivacyStatementUrl);
                                    break;
                            }
                        }
                    }));
            }
        }

        private SettingsProvider Settings => App.Settings;

        public LocationSettingsVM()
        {
            IsLocationEnabled = Settings.IsLocationEnabled;
        }
    }
}
