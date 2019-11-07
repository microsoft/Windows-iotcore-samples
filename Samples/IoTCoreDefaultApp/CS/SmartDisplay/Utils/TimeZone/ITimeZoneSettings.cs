// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.System;

namespace SmartDisplay.Utils
{
    public interface ITimeZoneSettings
    {
        IAsyncOperation<AutoUpdateTimeZoneStatus> AutoUpdateTimeZoneAsync(TimeSpan timeout);

        //
        // Summary:
        //     Changes the time zone using the display name.
        //
        // Parameters:
        //   timeZoneDisplayName:
        //     The display name of the time zone to change to.
        void ChangeTimeZoneByDisplayName(string timeZoneDisplayName);

        //
        // Summary:
        //     Gets whether the time zone can be changed.
        //
        // Returns:
        //     True if the time zone can be changed; otherwise, false.
        bool CanChangeTimeZone { get; }

        //
        // Summary:
        //     Gets the display name of the current time zone.
        //
        // Returns:
        //     The display name of the current time zone.

        string CurrentTimeZoneDisplayName { get; }

        //
        // Summary:
        //     Gets the display names for all supported time zones.
        //
        // Returns:
        //     The display names for all supported time zones.
        IReadOnlyList<string> SupportedTimeZoneDisplayNames { get; }
    }
}
