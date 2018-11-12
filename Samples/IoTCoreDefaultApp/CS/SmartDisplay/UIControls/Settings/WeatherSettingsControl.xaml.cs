// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public sealed partial class WeatherSettingsControl : SettingsUserControlBase
    {
        public WeatherSettingsControlVM ViewModel { get; } = new WeatherSettingsControlVM();
        protected override SettingsBaseViewModel ViewModelImpl => ViewModel;

        public WeatherSettingsControl()
        {
            InitializeComponent();
        }
    }
}
