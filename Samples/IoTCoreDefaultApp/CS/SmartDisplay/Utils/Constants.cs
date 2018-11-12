// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay
{
    public static class Constants
    {
        public const string GuidDevInterfaceUsbDevice = "A5DCBF10-6530-11D2-901F-00C04FB951ED";
        public const string BluetoothServiceUuid = "17890000-0068-0069-1532-1992D79BE4D8";
        public const string WODUrl = "WindowsOnDevices.com";
        public const string IoTHacksterUrl = "Hackster.io/windowsiot";
        public const string IoTGitHubUrl = "Github.com/ms-iot";
        public const string BingHomePageUrl = "Bing.com";

        // Guid used for ETW tracing
        public const string EtwProviderGuid = "06D538A0-59B9-4C8F-8379-0023DD58DC60";

        // Provider name for telemetry and ETW logging
        public const string EtwProviderName = "Microsoft.Windows.IoT.SmartDisplay";

        public static string HasDoneOOBEKey = "DefaultAppHasDoneOOBE";
        public static string HasDoneOOBEValue = "YES";

        // Settings subpanel width
        public const double SettingsWidth = 400;

        // Side pane content width
        public const double DefaultSidePaneContentWidth = 350;

        public const string PrivacyStatementUrl = "https://go.microsoft.com/fwlink/p/?linkid=850633";
        public const string PrivacyLearnMoreUrl = "https://go.microsoft.com/fwlink/p/?linkid=614828";
        public const string LocationPrivacyStatementUrl = "https://go.microsoft.com/fwlink/p/?linkid=850483";
    }

    public static class AuthConstants
    {
        // Resource for getting token to access Microsoft Graph APIs
        public const string GraphResource = "https://graph.microsoft.com";
        
        // Configuration for authenticating MSA
        public const string MsaClientId = "none";
        public const string MsaAuthority = "consumers";
        public const string MsaScope = "wl.basic";
        public const string MsaProviderId = "https://login.microsoft.com";
        
    }
}
