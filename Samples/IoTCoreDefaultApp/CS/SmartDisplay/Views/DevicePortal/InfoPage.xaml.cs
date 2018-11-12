// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.DevicePortal;

namespace SmartDisplay.Views.DevicePortal
{
    /// <summary>
    /// Displays OS info provided by WDP
    /// </summary>
    public sealed partial class InfoPage : PageBase
    {
        public InfoPageVM ViewModel { get; } = new InfoPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public InfoPage()
        {
            InitializeComponent();
        }
    }
}
