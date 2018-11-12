// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;

namespace SmartDisplay.Views.Settings
{
    public sealed partial class LocationSettings : PageBase
    {
        public LocationSettingsVM ViewModel { get; } = new LocationSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public LocationSettings()
        {
            InitializeComponent();
        }
    }
}
