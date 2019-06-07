//
// Copyright (c) Microsoft. All rights reserved.
//

namespace EdgeModuleSamples.Common
{
    public enum Orientation
    {
        Unknown = 0,
        RightSideUp,
        UpsideDown
    }

    namespace Messages
    {
        public static class Keys
        {
            public readonly static string AudioLoopbackModuleId = "AudioLoopback";
            public readonly static string Configuration = "Configuration";
            public readonly static string Desired = "Desired";
            public readonly static string DeviceIdMetadata = "iothub-connection-device-id";
            public readonly static string ModuleIdMetadata = "iothub-connection-module-id";
            public readonly static string FruitMaster = "FruitMaster";
            public readonly static string FruitModuleId = "WinML";
            public readonly static string FruitSeen = "FruitSeen";
            public readonly static string FruitSlaves = "FruitSlaves";
            public readonly static string FruitTest = "FruitTest";
            public readonly static string GPIOModuleId = "GPIO";
            public readonly static string I2CModuleId = "I2C";
            public readonly static string HubConnectionString = "HubConnectionString";
            public readonly static string InputFruit = "inputfruit";
            public readonly static string InputOrientation = "inputorientation";
            public readonly static string iothubMessageSchema = "iothub-message-schema";
            public readonly static string MessageCreationUTC = "iothub-creation-time-utc";
            public readonly static string ModuleLoadedInputRoute = "inputModule";
            public readonly static string ModuleLoadedOutputRouteLocal0 = "outputModuleLocal0";
            public readonly static string ModuleLoadedOutputRouteLocal1 = "outputModuleLocal1";
            public readonly static string ModuleLoadedOutputRouteUpstream = "outputModuleUpstream";
            public readonly static string Orientation = "Orientation";
            public readonly static string OutputFruit0 = "outputfruit0"; // gpio
            public readonly static string OutputFruit1 = "outputfruit1"; // uart
            public readonly static string OutputFruit2 = "outputfruit2"; // pwm
            public readonly static string OutputOrientation = "outputorientation";
            public readonly static string OutputUpstream = "outputupstream";
            public readonly static string PWMModuleId = "PWM";
            public readonly static string SetFruit = "SetFruit";
            public readonly static string SetModuleLoaded = "SetModuleLoaded";
            public readonly static string SetOrientation = "SetOrientation";
            public readonly static string SPIModuleId = "SPI";
            public readonly static string Reported = "Reported";
            public readonly static string twinChangeNotification = "twinChangeNotification";
            public readonly static string UARTModuleId = "UART";
            public readonly static string WinMLModuleId = "WinML";
        };
        public class AzureMessageBase
        {
        }
        public class OrderedMessage : AzureMessageBase
        {
            public string OriginalEventUTCTime { get; set; }

        }
        public class FruitMessage : OrderedMessage
        {
            public string FruitSeen { get; set; }
        }
        public class ModuleLoadedMessage : AzureMessageBase
        {
            public string ModuleName { get; set; }
        }

        public class OrientationMessage : OrderedMessage
        {
            public Orientation OrientationState { get; set; }
        };
    }
}
