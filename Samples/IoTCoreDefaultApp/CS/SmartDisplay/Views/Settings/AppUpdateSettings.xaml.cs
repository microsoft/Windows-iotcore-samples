// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains app update settings for the app
    /// </summary>
    public partial class AppUpdateSettings : PageBase
    {
        public AppUpdateSettingsVM ViewModel { get; } = new AppUpdateSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public AppUpdateSettings() : base()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.SetUpVM();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.TearDownVM();
        }
    }
}
