// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public sealed partial class TileSettingsControl : SettingsUserControlBase
    {
        public TileSettingsControlVM ViewModel { get; } = new TileSettingsControlVM();
        protected override SettingsBaseViewModel ViewModelImpl => ViewModel;

        public TileSettingsControl() : base()
        {
            InitializeComponent();
        }
    }
}
