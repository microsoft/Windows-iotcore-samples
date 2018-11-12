// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains system settings related to language and time localization
    /// </summary>
    public sealed partial class PrivacySettings : PageBase
    {
        public DiagnosticSettingsVM ViewModel { get; } = new DiagnosticSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public PrivacySettings()
        {
            InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.SetUpVM();
        }
    }
}
