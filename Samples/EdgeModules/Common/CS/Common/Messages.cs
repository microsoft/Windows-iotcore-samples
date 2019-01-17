//
// Copyright (c) Microsoft. All rights reserved.
//

namespace EdgeModuleSamples.Common.Messages
{
    public static class Keys
    {
        public readonly static string Desired = "Desired";
        public readonly static string FruitSeen = "FruitSeen";
        public readonly static string FruitMaster = "FruitMaster";
        public readonly static string FruitModuleId = "WinML";
        public readonly static string GPIOModuleId = "GPIO";
        public readonly static string HubConnectionString = "HubConnectionString";
        public readonly static string Reported = "Reported";
    };
    public class FruitMessage
    {
        public string FruitSeen { get; set; }
    }
    public class ModuleLoadMessage
    {
        public string ModuleName { get; set; }
    }
}
