//
// Copyright (c) Microsoft. All rights reserved.
//

namespace EdgeModuleSamples.Common.Messages
{
    public static class Keys
    {
        public readonly static string Configuration = "Configuration";
        public readonly static string Desired = "Desired";
        public readonly static string FruitMaster = "FruitMaster";
        public readonly static string FruitModuleId = "WinML";
        public readonly static string FruitSeen = "FruitSeen";
        public readonly static string FruitTest = "FruitTest";
        public readonly static string GPIOModuleId = "GPIO";
        public readonly static string HubConnectionString = "HubConnectionString";
        public readonly static string InputFruit = "inputfruit";
        public readonly static string OutputFruit = "outputfruit";
        public readonly static string OutputUpstream = "outputupstream";
        public readonly static string SetFruit = "SetFruit";
        public readonly static string Reported = "Reported";
    };
    public class AzureMessageBase
    {
    }
    public class FruitMessage : AzureMessageBase
    {
        public string FruitSeen { get; set; }
    }
    public class ModuleLoadMessage : AzureMessageBase
    {
        public string ModuleName { get; set; }
    }
}
