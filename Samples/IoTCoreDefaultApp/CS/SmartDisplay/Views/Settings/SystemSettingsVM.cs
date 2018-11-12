// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.ObjectModel;
using Windows.Foundation.Diagnostics;
using Windows.System;

namespace SmartDisplay.ViewModels.Settings
{
    public class SystemSettingsVM : BaseViewModel
    {
        #region UI language properties

        public bool DisplayLanguageUpdateEnabled
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string CurrentLanguageDisplayName
        {
            get { return GetStoredProperty<string>() ?? LanguageManager.GetCurrentLanguageDisplayName(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    UpdateSystemLanguageSettings(value);
                }
            }
        }

        public ObservableCollection<string> DisplayLanguageNamesCollection
        {
            get { return GetStoredProperty<ObservableCollection<string>>() ?? new ObservableCollection<string>(); }
            private set { SetStoredProperty(value); }
        }

        #endregion

        #region Input language properties

        public bool InputLanguageUpdateEnabled
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string CurrentInputLanguageDisplayName
        {
            get { return GetStoredProperty<string>() ?? LanguageManager.GetCurrentInputLanguageDisplayName(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    UpdateSystemLanguageSettings(value, true);
                }
            }
        }

        public ObservableCollection<string> InputLanguageNamesCollection
        {
            get { return GetStoredProperty<ObservableCollection<string>>() ?? new ObservableCollection<string>(); }
            private set { SetStoredProperty(value); }
        }

        #endregion

        #region Time localization properties

        public bool TimeZoneUpdateEnabled
        {
            get { return GetStoredProperty<bool>(); }
            private set { SetStoredProperty(value); }
        }

        public string CurrentTimeZoneDisplayName
        {
            get { return GetStoredProperty<string>() ?? TimeZoneSettings.CurrentTimeZoneDisplayName; }
            set
            {
                if (SetStoredProperty(value))
                {
                    UpdateTimeZone(value);
                }
            }
        }

        public ObservableCollection<string> TimeZoneNamesCollection
        {
            get { return GetStoredProperty<ObservableCollection<string>>() ?? new ObservableCollection<string>(); }
            private set { SetStoredProperty(value); }
        }

        #endregion

        #region Page properties

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            private set { SetStoredProperty(value); }
        }

        #endregion

        #region Page services and providers

        private LanguageManager LanguageManager => LanguageManager.GetInstance();
        private ILogService LogService => AppService?.LogService;
        private SettingsProvider SettingsManager => AppService?.Settings as SettingsProvider;
        private ITelemetryService TelemetryService => AppService?.TelemetryService;

        #endregion

        #region Localized strings and private fields

        private string InputLanguageUpdateSuccessText { get; } = Common.GetLocalizedText("InputLanguageUpdateSuccessText");
        private string DisplayLanguageUpdateSuccessText { get; } = Common.GetLocalizedText("DisplayLanguageUpdateSuccessText");
        private string SystemSettingsUpdateErrorText { get; } = Common.GetLocalizedText("SystemSettingsUpdateErrorText");
        private string TimeZoneUpdateSuccessText { get; } = Common.GetLocalizedText("TimeZoneUpdateSuccessText");

        private bool _updateSuccess = false;
        private string _resultsText = string.Empty;

        #endregion

        public SystemSettingsVM() : base()
        {
            Width = Constants.SettingsWidth;

            DisplayLanguageNamesCollection = new ObservableCollection<string>();
            InputLanguageNamesCollection = new ObservableCollection<string>();
            TimeZoneNamesCollection = new ObservableCollection<string>();
        }

        public void SetUpVM()
        {
            LoadSystemSettings();
        }

        private void LoadSystemSettings()
        {
            // Load supported UI languages
            foreach (string language in LanguageManager.LanguageDisplayNames)
            {
                if (!DisplayLanguageNamesCollection.Contains(language))
                {
                    DisplayLanguageNamesCollection.Add(language);
                }
            }

            // Load supported keyboard languages
            foreach (string inputLanguage in LanguageManager.InputLanguageDisplayNames)
            {
                if (!InputLanguageNamesCollection.Contains(inputLanguage))
                {
                    InputLanguageNamesCollection.Add(inputLanguage);
                }
            }

            // Load timezones
            foreach (string timeZone in TimeZoneSettings.SupportedTimeZoneDisplayNames)
            {
                if (!TimeZoneNamesCollection.Contains(timeZone))
                {
                    TimeZoneNamesCollection.Add(timeZone);
                }
            }
            
            // Only enable updating if more than one choice is available
            DisplayLanguageUpdateEnabled = (DisplayLanguageNamesCollection.Count > 1);
            InputLanguageUpdateEnabled = (InputLanguageNamesCollection.Count > 1);
            TimeZoneUpdateEnabled = (TimeZoneNamesCollection.Count > 1);
        }

        /// <summary>
        /// Tries to update the current language settings using Windows.Globalization and displays a notification of the result
        /// </summary>
        /// <param name="languageDisplayName">String display name of the new language to be set</param>
        /// <param name="updateInput">True for input (keyboard) languages, default is false for display (UI) languages</param>
        /// <returns>True if the operation is successful, otherwise false</returns>
        public bool UpdateSystemLanguageSettings(string languageDisplayName, bool updateInput = false)
        {
            _resultsText = SystemSettingsUpdateErrorText;

            try
            {
                if (updateInput && LanguageManager.UpdateInputLanguage(languageDisplayName))
                {
                    _updateSuccess = true;
                    _resultsText = string.Format(InputLanguageUpdateSuccessText, languageDisplayName);
                }
                else if (!updateInput && LanguageManager.UpdateLanguage(languageDisplayName))
                {
                    _updateSuccess = true;
                    _resultsText = string.Format(DisplayLanguageUpdateSuccessText, languageDisplayName);
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), LoggingLevel.Error);
            }
            finally
            {
                PageService?.ShowNotification(_resultsText, 2000);
            }

            return _updateSuccess;
        }

        /// <summary>
        /// Tries to update the current time zone within Windows System Settings and displays a notification of the result
        /// </summary>
        /// <param name="timeZoneName">String display name of the new timezone to be set</param>
        /// <returns>True if the operation is successful, otherwise false</returns>
        public bool UpdateTimeZone(string timeZoneName)
        {
            _resultsText = SystemSettingsUpdateErrorText;

            try
            {
                if (TimeZoneSettings.CanChangeTimeZone)
                {
                    TimeZoneSettings.ChangeTimeZoneByDisplayName(timeZoneName);

                    if (timeZoneName == TimeZoneSettings.CurrentTimeZoneDisplayName)
                    {
                        _updateSuccess = true;
                        _resultsText = string.Format(TimeZoneUpdateSuccessText, timeZoneName);
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), LoggingLevel.Error);
            }
            finally
            {
                PageService?.ShowNotification(_resultsText, 2000);
            }

            return _updateSuccess;
        }
    }
}
