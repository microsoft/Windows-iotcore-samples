// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public class SlideshowSettingsControlVM : SmartDisplaySettingsBaseViewModel
    {
        public string SlideshowIntervalSeconds
        {
            get { return Settings.SlideshowIntervalSeconds.ToString(); }
            set
            {
                RemoveInvalidProperty();
                TelemetryService.WriteEvent("SlideshowIntervalTextBoxTextChanged");

                if (int.TryParse(value, out int temp) && temp > 0)
                {
                    SettingsProvider.SaveSetting(temp);
                    return;
                }

                AddInvalidProperty(string.Format(InvalidValueText, value));
            }
        }
    }
}
