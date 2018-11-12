// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public sealed partial class BrowserSettingsControl : SettingsUserControlBase
    {
        public BrowserSettingsControlVM ViewModel { get; } = new BrowserSettingsControlVM();
        protected override SettingsBaseViewModel ViewModelImpl => ViewModel;

        public BrowserSettingsControl()
        {
            InitializeComponent();
        }
    }
}
