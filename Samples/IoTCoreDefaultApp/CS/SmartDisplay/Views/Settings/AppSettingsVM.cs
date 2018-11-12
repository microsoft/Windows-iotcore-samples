// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartDisplay.ViewModels.Settings
{
    public class AppSettingsVM : SmartDisplaySettingsBaseViewModel
    {
        #region UI Properties

        public ObservableCollection<string> PagesCollection
        {
            get { return GetStoredProperty<ObservableCollection<string>>(); }
            set { SetStoredProperty(value); }
        }

        public string EtwProviderGuid
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool AreTelemetryControlsVisible { get; set; }

        #endregion

        #region Settings Properties

        // Note: Actual property names need to be used in the Getters in order
        // to populate the default values specified in Settings on new install
        
        public string AppDefaultPage
        {
            get { return PageUtil.GetDescriptorFromTypeFullName(Settings.AppDefaultPage)?.Title ?? string.Empty; }
            set
            {
                var pageType = PageUtil.GetDescriptorFromTitle(value)?.Type;
                if (pageType != null)
                {
                    Settings.AppDefaultPage = pageType.FullName;
                    TelemetryService.WriteEvent("DefaultPageComboBoxSelectionChanged");
                }
                else
                {
                    AddInvalidProperty(string.Format(SetDefaultPageErrorText, value));
                }
            }
        }

        public bool ScreensaverEnabled
        {
            get { return Settings.ScreensaverEnabled; }
            set { Settings.SaveSetting(value); }
        }

        public bool AppEnableTelemetry
        {
            get { return Settings.AppEnableTelemetry; }
            set { Settings.SaveSetting(value); }
        }

        #endregion

        #region Localized strings

        private string EtwProviderGUIDLabelText { get; } = Common.GetLocalizedText("EtwProviderGUIDLabelText");
        private string InvalidNumericalValueErrorText { get; } = Common.GetLocalizedText("InvalidNumericalValueErrorText");
        private string SetDefaultPageErrorText { get; } = Common.GetLocalizedText("SetDefaultPageErrorText");

        #endregion

        public void SetUpVM()
        {
            // Show telemetry controls if there are any telemetry services loaded
            AreTelemetryControlsVisible = AppComposer.Imports.TelemetryServices.Count() > 0;

            EtwProviderGuid = string.Format(EtwProviderGUIDLabelText, Constants.EtwProviderGuid);
            PagesCollection = new ObservableCollection<string>(PageUtil.GetFullPageList().Select(x => x.Title));

            Settings.SettingsUpdated += Settings_SettingsUpdated;
        }

        public void TearDownVM()
        {
            Settings.SettingsUpdated -= Settings_SettingsUpdated;
        }

        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            NotifyPropertyChanged(args.Key);
        }
    }
}
