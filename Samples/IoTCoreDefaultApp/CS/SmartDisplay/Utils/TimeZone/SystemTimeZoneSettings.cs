// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;

namespace SmartDisplay.Utils
{
    public class SystemTimeZoneSettings : ITimeZoneSettings
    {
        public bool CanChangeTimeZone => TimeZoneSettings.CanChangeTimeZone;

        public string CurrentTimeZoneDisplayName => TimeZoneSettings.CurrentTimeZoneDisplayName;

        public IReadOnlyList<string> SupportedTimeZoneDisplayNames => TimeZoneSettings.SupportedTimeZoneDisplayNames;

        public IAsyncOperation<AutoUpdateTimeZoneStatus> AutoUpdateTimeZoneAsync(TimeSpan timeout) => TimeZoneSettings.AutoUpdateTimeZoneAsync(timeout);

        public void ChangeTimeZoneByDisplayName(string timeZoneDisplayName) => TimeZoneSettings.ChangeTimeZoneByDisplayName(timeZoneDisplayName);
    }
}
