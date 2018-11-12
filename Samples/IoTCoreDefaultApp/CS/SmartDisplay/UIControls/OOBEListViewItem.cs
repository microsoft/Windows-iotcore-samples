// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public class OOBEListViewItem
    {
        public string Title;
        public string Description;
        public string Icon;
    }

    public class TelemetryLevelDisplay : OOBEListViewItem
    {
        public int Level;
    }

    public class LocationDisplayListViewItem : OOBEListViewItem
    {
        public bool IsAllowed;
    }
}
