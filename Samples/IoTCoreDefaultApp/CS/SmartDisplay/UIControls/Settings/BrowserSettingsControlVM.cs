// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Controls
{
    public class BrowserSettingsControlVM : SmartDisplaySettingsBaseViewModel
    {
        public string BrowserHomePage
        {
            get { return Settings.BrowserHomePage; }
            set
            {
                RemoveInvalidProperty();

                if (!string.IsNullOrWhiteSpace(value))
                {
                    SettingsProvider.SaveSetting(value);
                    return;
                }

                AddInvalidProperty(string.Format(InvalidValueText, value));
            }
        }
    }
}
