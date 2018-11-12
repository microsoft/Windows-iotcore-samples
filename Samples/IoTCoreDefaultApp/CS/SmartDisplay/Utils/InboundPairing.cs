// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.Devices.Enumeration;

namespace SmartDisplay.Bluetooth
{
    public class InboundPairingEventArgs : EventArgs
    {
        public DeviceInformation DeviceInfo { get; private set; }
        public InboundPairingEventArgs(DeviceInformation di)
        {
            DeviceInfo = di;
        }
    }

    // Callback handler delegate type for Inbound pairing requests
    public delegate void InboundPairingRequestedHandler(object sender, InboundPairingEventArgs e);
}
