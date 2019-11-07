// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;

namespace SmartDisplay.Utils
{
    public class EmptyTimeZoneSettings : ITimeZoneSettings
    {
        public bool CanChangeTimeZone => false;

        public string CurrentTimeZoneDisplayName => null;

        public IReadOnlyList<string> SupportedTimeZoneDisplayNames { get; } = new List<string>();

        public IAsyncOperation<AutoUpdateTimeZoneStatus> AutoUpdateTimeZoneAsync(TimeSpan timeout) => null;

        public void ChangeTimeZoneByDisplayName(string timeZoneDisplayName)
        {

        }
    }
}
