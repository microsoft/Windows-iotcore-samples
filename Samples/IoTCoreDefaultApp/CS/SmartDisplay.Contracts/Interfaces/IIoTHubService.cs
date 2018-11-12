// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation;

namespace SmartDisplay.Contracts
{
    public interface IIoTHubService : ISmartDisplayService
    {
        event TypedEventHandler<object, DesiredPropertyUpdatedEventArgs> DesiredPropertyUpdated;

        string HostName { get; }

        string DeviceId { get; }

        bool IsDeviceClientConnected { get; }

        Task ConnectAsync();

        Task SendEventAsync(byte[] data);

        Task AcknowledgeDesiredPropertyChangeAsync(string desiredProperty, object desiredValue, object version = null);
    }

    public class DesiredPropertyUpdatedEventArgs : EventArgs
    {
        public Dictionary<string, object> DesiredProperties;
        public object UserContext;
    }
}
