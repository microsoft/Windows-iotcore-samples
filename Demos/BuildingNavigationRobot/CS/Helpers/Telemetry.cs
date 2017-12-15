// Copyright (c) Microsoft. All rights reserved.

using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AzureIoTHub
{
    class Telemetry
    {
        private static string DeviceId = "YOUR_DEVICE_ID";
        private static string HubUri = "YOUR_AZURE_IOT_HUB_URI";
        private static string ConnectionKey = "YOUR_AZURE_IOT_HUB_CONNECTION_KEY";

        private static DeviceClient deviceClient;

        public static async void SendReport(MessageType type, string message)
        {
            var guid = Guid.NewGuid();

            if (deviceClient == null)
            {
                Debug.WriteLine("Created new instance of DeviceClient!");
                deviceClient = DeviceClient.Create(HubUri,
                    AuthenticationMethodFactory.CreateAuthenticationWithRegistrySymmetricKey(DeviceId, ConnectionKey),
                    TransportType.Http1);
            }

            var str = $"{{\"deviceId\":\"{DeviceId}\",\"guid\":\"{guid.ToString()}\",\"type\":\"{TypeString(type)}\",\"message\":\"{message}\"}}";

            var messageJson = new Message(Encoding.ASCII.GetBytes(str));

            await deviceClient.SendEventAsync(messageJson);
            Debug.WriteLine("Telemetry data sent: {0} -> {1}", TypeString(type), message);
        }

        private static string TypeString(MessageType type)
        {
            switch (type)
            {
                case MessageType.VoiceCommand:
                    return "VoiceCommand";
                case MessageType.Navigation:
                    return "Navigation";
                case MessageType.Positioning:
                    return "Positioning";
                case MessageType.Collision:
                    return "Collision";
                default:
                    return "Error error";
            }
        }

        public enum MessageType
        {
            VoiceCommand, Navigation, Positioning, Collision
        }

    }
}
