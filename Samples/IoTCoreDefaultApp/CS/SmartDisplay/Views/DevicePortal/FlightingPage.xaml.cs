// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.DevicePortal;

namespace SmartDisplay.Views.DevicePortal
{
    /// <summary>
    /// Displays flighting information and allows user to change rings
    /// </summary>
    public sealed partial class FlightingPage : PageBase
    {
        public FlightingPageVM ViewModel { get; } = new FlightingPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public FlightingPage()
        {
            InitializeComponent();
        }
    }
}
