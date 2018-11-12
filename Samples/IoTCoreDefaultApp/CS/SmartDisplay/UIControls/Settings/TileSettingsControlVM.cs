// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using Windows.UI;

namespace SmartDisplay.Controls
{
    public class TileSettingsControlVM : SmartDisplaySettingsBaseViewModel
    {
        private string InvalidNumericalValueErrorText { get; } = Common.GetLocalizedText("InvalidNumericalValueErrorText");
        
        public Color TileColor
        {
            get { return Settings.TileColor; }
            set { Settings.TileColor = value; }
        }

        public string AppScaling
        {
            get { return Settings.AppScaling.ToString(); }
            set
            {
                RemoveInvalidProperty();

                if (double.TryParse(value, out double temp) && temp > 0)
                {
                    Settings.SaveSetting(temp);
                    return;
                }

                AddInvalidProperty(string.Format(InvalidNumericalValueErrorText, value));
            }
        }

        public bool AppAutoScaling
        {
            get { return Settings.AppAutoScaling; }
            set { Settings.SaveSetting(value); }
        }

        public string AppTileWidth
        {
            get { return Settings.AppTileWidth.ToString(); }
            set
            {
                RemoveInvalidProperty();

                if (double.TryParse(value, out double temp) && temp > 0)
                {
                    Settings.SaveSetting(temp);
                    return;
                }

                AddInvalidProperty(string.Format(InvalidValueText, value));
            }
        }

        public string AppTileHeight
        {
            get { return Settings.AppTileHeight.ToString(); }
            set
            {
                RemoveInvalidProperty();

                if (double.TryParse(value, out double temp) && temp > 0)
                {
                    Settings.SaveSetting(temp);
                    return;
                }

                AddInvalidProperty(string.Format(InvalidValueText, value));
            }
        }

        public bool UseMDL2Icons
        {
            get { return Settings.UseMDL2Icons; }
            set { Settings.SaveSetting(value); }
        }
    }
}
