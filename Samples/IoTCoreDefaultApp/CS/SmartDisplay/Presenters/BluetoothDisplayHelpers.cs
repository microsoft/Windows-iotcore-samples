// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using System.Collections.Generic;
using System.ComponentModel;
using Windows.Devices.Enumeration;
using Windows.UI.Xaml;

// NOTE: The following using statements are only needed in order to demonstrate many of the
// different device selectors available from Windows Runtime APIs. You will only need to include
// the namespace for the Windows Runtime API your actual scenario needs.
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.HumanInterfaceDevice;
using Windows.Devices.Sensors;
using Windows.Devices.WiFiDirect;
using Windows.Media.Casting;
using Windows.Media.DialProtocol;
using Windows.Networking.Proximity;

namespace SmartDisplay.Bluetooth
{
    public class BluetoothDeviceInfo : INotifyPropertyChanged
    {
        private static string pairingPairedStateString = Common.GetLocalizedText("BluetoothDeviceStatePairedText");
        private static string pairingReadyToPairStateString = Common.GetLocalizedText("BluetoothDeviceStateReadyToPairText");
        private static string pairingUnknownStateString = Common.GetLocalizedText("BluetoothDeviceStateUnknownText");
        private static string deviceNameUnknownString = Common.GetLocalizedText("BluetoothDeviceNameUnknownText");

        public BluetoothDeviceInfo(DeviceInformation incomingDeviceInfo)
        {
            DeviceInformation = incomingDeviceInfo;
        }

        public DeviceInformation DeviceInformation { get; private set; }
        public string Id => DeviceInformation.Id;
        public string IdWithoutProtocolPrefix
        {
            get
            {
                if (!string.IsNullOrEmpty(DeviceInformation.Id))
                {
                    // Trim the protocol to display the Bluetooth Id
                    int colonIndex = DeviceInformation.Id.IndexOf(":");
                    if (colonIndex >= 2)
                    {
                        return DeviceInformation.Id.Substring(colonIndex - 2).ToUpper();
                    }
                }
                return string.Empty;
            }
        }
        public string Name => string.IsNullOrWhiteSpace(DeviceInformation.Name) ? deviceNameUnknownString : DeviceInformation.Name;
        public DeviceInformationKind Kind => DeviceInformation.Kind;
        public bool CanPair => DeviceInformation.Pairing.CanPair;
        public bool IsPaired => DeviceInformation.Pairing.IsPaired;
        public Visibility PairButtonVisibility => (!DeviceInformation.Pairing.IsPaired && DeviceInformation.Pairing.CanPair) ? Visibility.Visible : Visibility.Collapsed;
        public Visibility UnpairButtonVisibility => DeviceInformation.Pairing.IsPaired ? Visibility.Visible : Visibility.Collapsed;
        public IReadOnlyDictionary<string, object> Properties => DeviceInformation.Properties;

        public string DevicePairingStateText
        {
            get
            {
                if (!DeviceInformation.Pairing.IsPaired && DeviceInformation.Pairing.CanPair)
                {
                    return pairingReadyToPairStateString;
                }
                else if (DeviceInformation.Pairing.IsPaired)
                {
                    return pairingPairedStateString;
                }
                else
                {
                    return pairingUnknownStateString;
                }
            }
        }

        private static readonly string[] NotifyProperties = new string[]
        {
            "DeviceInformation",
            "Id",
            "IdWithoutProtocolPrefix",
            "Name",
            "Kind",
            "CanPair",
            "IsPaired",
            "PairButtonVisibility",
            "UnpairButtonVisibility",
            "DevicePairingStateText",
        };

        public void Update(DeviceInformationUpdate deviceInfoUpdate)
        {
            DeviceInformation.Update(deviceInfoUpdate);

            foreach (var property in NotifyProperties)
            {
                NotifyPropertyChanged(property);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
