// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.ObjectModel;
using Windows.Devices.Enumeration;
using Windows.UI.Core;

namespace SmartDisplay.Utils
{
    public class ConnectedDevicePresenter
    {
        private const string UsbDevicesSelector = "(System.Devices.InterfaceClassGuid:=\"{" + Constants.GuidDevInterfaceUsbDevice + "}\")";

        private CoreDispatcher _dispatcher;
        private ObservableCollection<string> _devices = new ObservableCollection<string>();
        private DeviceWatcher _usbConnectedDevicesWatcher;

        public ConnectedDevicePresenter(CoreDispatcher dispatcher)
        {
            _dispatcher = dispatcher;

            _usbConnectedDevicesWatcher = DeviceInformation.CreateWatcher(UsbDevicesSelector);
            _usbConnectedDevicesWatcher.EnumerationCompleted += DevicesEnumCompleted;
            _usbConnectedDevicesWatcher.Updated += DevicesUpdated;
            _usbConnectedDevicesWatcher.Removed += DevicesRemoved;
            _usbConnectedDevicesWatcher.Start();
        }

        private async void DevicesEnumCompleted(DeviceWatcher sender, object args)
        {
            App.LogService.Write("USB Devices Enumeration Completed");

            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateDevices();
            });
        }

        private async void DevicesUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            App.LogService.Write("Updated USB device: " + args.Id);

            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateDevices();
            });
        }

        private async void DevicesRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            App.LogService.Write("Removed USB device: " + args.Id);

            await _dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
            {
                UpdateDevices();
            });
        }

        private async void UpdateDevices()
        {
            // Get a list of all enumerated usb devices              
            var deviceInformationCollection = await DeviceInformation.FindAllAsync(UsbDevicesSelector);

            // Always start with a clean list                 
            _devices.Clear();

            if (deviceInformationCollection == null || deviceInformationCollection.Count == 0)
            {
                return;
            }

            // If devices are found, enumerate them and add only enabled ones
            foreach (var device in deviceInformationCollection)
            {
                if (device.IsEnabled && !_devices.Contains(device.Name))
                {
                    _devices.Add(device.Name);
                }
            }
        }

        public ObservableCollection<string> GetConnectedDevices()
        {
            return _devices;
        }
    }
}
