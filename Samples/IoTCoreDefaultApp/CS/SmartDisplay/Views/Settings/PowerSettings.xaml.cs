// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains device power-related settings for the app
    /// </summary>
    public sealed partial class PowerSettings : PageBase
    {
        public PowerSettingsVM ViewModel { get; } = new PowerSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public PowerSettings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}
