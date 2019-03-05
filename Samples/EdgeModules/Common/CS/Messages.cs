//
// Copyright (c) Microsoft. All rights reserved.
//

namespace EdgeModuleSamples.Common
{
    public enum Orientation
    {
        RightSideUp,
        UpsideDown
    }

    namespace Messages
    {
        public static class Keys
        {
            public readonly static string Configuration = "Configuration";
            public readonly static string Desired = "Desired";
            public readonly static string DeviceIdMetadata = "iothub-connection-device-id";
            public readonly static string FruitMaster = "FruitMaster";
            public readonly static string FruitModuleId = "WinML";
            public readonly static string FruitSeen = "FruitSeen";
            public readonly static string FruitSlaves = "FruitSlaves";
            public readonly static string FruitTest = "FruitTest";
            public readonly static string GPIOModuleId = "GPIO";
            public readonly static string I2cModuleId = "I2c";
            public readonly static string HubConnectionString = "HubConnectionString";
            public readonly static string InputFruit = "inputfruit";
            public readonly static string InputOrientation0 = "inputorientation0";
            public readonly static string InputOrientation1 = "inputorientation1";
            public readonly static string iothubMessageSchema = "iothub-message-schema";
            public readonly static string MessageCreationUTC = "iothub-creation-time-utc";
            public readonly static string ModuleLoadInputRoute = "inputModule";
            public readonly static string ModuleLoadOutputRouteLocal0 = "outputModuleLocal0";
            public readonly static string ModuleLoadOutputRouteLocal1 = "outputModuleLocal1";
            public readonly static string ModuleLoadOutputRouteUpstream = "outputModuleUpstream";
            public readonly static string Orientation = "Orientation";
            public readonly static string OutputFruit0 = "outputfruit0";
            public readonly static string OutputFruit1 = "outputfruit1";
            public readonly static string OutputOrientation = "outputorientation";
            public readonly static string OutputUpstream = "outputupstream";
            public readonly static string SetFruit = "SetFruit";
            public readonly static string Reported = "Reported";
            public readonly static string twinChangeNotification = "twinChangeNotification";
            public readonly static string UARTModuleId = "UART";
        };
        public class AzureMessageBase
        {
        }
        public class FruitMessage : AzureMessageBase
        {
            public string OriginalEventUTCTime { get; set; }
            public string FruitSeen { get; set; }
        }
        public class ModuleLoadMessage : AzureMessageBase
        {
            public string ModuleName { get; set; }
        }

        public class OrientationMessage : AzureMessageBase
        {
            public string OriginalEventUTCTime { get; set; }
            public Orientation OrientationState;
        };
    }
}
