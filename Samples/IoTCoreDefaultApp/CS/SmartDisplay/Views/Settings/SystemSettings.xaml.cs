// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using System.ComponentModel;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains system settings related to language and time localization
    /// </summary>
    public sealed partial class SystemSettings : PageBase
    {
        public SystemSettingsVM ViewModel { get; } = new SystemSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public SystemSettings()
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
