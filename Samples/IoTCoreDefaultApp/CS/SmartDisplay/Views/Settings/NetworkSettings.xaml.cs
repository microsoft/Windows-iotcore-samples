// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains networking settings for the app
    /// </summary>
    public partial class NetworkSettings : PageBase
    {
        public NetworkSettingsVM ViewModel { get; } = new NetworkSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public NetworkSettings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.SetUpVM();
        }
    }
}
