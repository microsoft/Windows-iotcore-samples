// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public sealed partial class SlideshowSettingsControl : SettingsUserControlBase
    {
        public SlideshowSettingsControlVM ViewModel { get; } = new SlideshowSettingsControlVM();
        protected override SettingsBaseViewModel ViewModelImpl => ViewModel;

        public SlideshowSettingsControl()
        {
            InitializeComponent();
        }
    }
}
