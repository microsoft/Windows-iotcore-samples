// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.DevicePortal;

namespace SmartDisplay.Views.DevicePortal
{
    /// <summary>
    /// Displays Windows Update status of device
    /// </summary>
    public sealed partial class WindowsUpdatePage : PageBase
    {
        public WindowsUpdatePageVM ViewModel { get; } = new WindowsUpdatePageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;
 
        public WindowsUpdatePage()
        {
            InitializeComponent();
        }
    }
}
