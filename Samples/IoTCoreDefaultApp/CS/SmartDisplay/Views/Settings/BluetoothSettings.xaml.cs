// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Bluetooth;
using SmartDisplay.Controls;
using SmartDisplay.ViewModels.Settings;
using Windows.System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains bluetooth settings for the app
    /// </summary>
    public sealed partial class BluetoothSettings : PageBase
    {
        public BluetoothSettingsVM ViewModel { get; } = new BluetoothSettingsVM();
        protected override ViewModels.BaseViewModel ViewModelImpl => ViewModel;

        public BluetoothSettings()
        {
            InitializeComponent();

            ViewModel.SetUpBluetooth();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.SetUpVM();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.TearDownVM();
        }

        /// <summary>
        /// Called when wanting to pair with the selected device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void PairButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button pairButton)
            {
                var currentDeviceInfo = pairButton.DataContext as BluetoothDeviceInfo;
                await ViewModel.PairingRequestedAsync(pairButton, currentDeviceInfo);
            }
        }

        /// <summary>
        /// Called when wanting to unpair from the selected device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void UnpairButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button unpairButton)
            {
                var currentDeviceInfo = unpairButton.DataContext as BluetoothDeviceInfo;
                await ViewModel.UnpairDeviceAsync(unpairButton, currentDeviceInfo);
            }
        }

        /// <summary>
        /// Called when user entered a confirmation PIN and pressed <Return> in the entry flyout
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PinEntryTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (sender is TextBox pinEntryBox && e.Key == VirtualKey.Enter)
            {
                //  Close the flyout and save the PIN the user entered
                if (!string.IsNullOrWhiteSpace(pinEntryBox.Text))
                {
                    ViewModel.AcceptPairingUsingInputPIN(pinEntryBox.Text);
                }
            }
        }
    }
}
