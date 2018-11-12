// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.DevicePortal;

namespace SmartDisplay.Views.DevicePortal
{
    /// <summary>
    /// Displays a list of packages that are installed on the device
    /// </summary>
    public sealed partial class PackagesPage : PageBase
    {
        public PackagesPageVM ViewModel { get; } = new PackagesPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public PackagesPage()
        {
            InitializeComponent();
        }
    }
}
