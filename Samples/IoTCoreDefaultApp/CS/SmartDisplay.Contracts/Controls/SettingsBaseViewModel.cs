// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.ViewModels;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    public class SettingsBaseViewModel : BaseViewModel
    {
        protected ISettingsProvider SettingsProvider => AppService?.Settings;
        protected ILogService LogService => AppService?.LogService;
        protected ITelemetryService TelemetryService => AppService?.TelemetryService;

        protected Dictionary<string, string> InvalidProperties = new Dictionary<string, string>();

        protected string InvalidValueText { get; } = ResourceLoader.GetForViewIndependentUse().GetString("InvalidValueText");

        #region UI and commands 

        public string Status => string.Join(Environment.NewLine, InvalidProperties.Values).Trim();

        public bool IsStatusVisible
        {
            get { return (!string.IsNullOrEmpty(Status)); }
        }

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public SolidColorBrush BackgroundColor
        {
            get { return GetStoredProperty<SolidColorBrush>() ?? new SolidColorBrush(Colors.Transparent); }
            set { SetStoredProperty(value); }
        }

        #endregion

        public SettingsBaseViewModel(IAppService appService) : base()
        {
            SetAppService(appService);
            Width = 350;
        }

        public void SetUpVMBase()
        {
            SettingsProvider.SettingsUpdated += Settings_SettingsUpdated;
        }

        public void TearDownVMBase()
        {
            SettingsProvider.SettingsUpdated -= Settings_SettingsUpdated;
        }

        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            NotifyPropertyChanged(args.Key);
        }

        protected void AddInvalidProperty(string errorText, [CallerMemberName] string propertyName = null)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                return;
            }

            if (InvalidProperties.ContainsKey(propertyName))
            {
                InvalidProperties[propertyName] = errorText;
            }
            else
            {
                InvalidProperties.Add(propertyName, errorText);
            }

            NotifyStatusChanged();
        }

        protected void RemoveInvalidProperty([CallerMemberName] string propertyName = null)
        {
            if (InvalidProperties.ContainsKey(propertyName))
            {
                InvalidProperties.Remove(propertyName);
                NotifyStatusChanged();
            }
        }

        protected void NotifyStatusChanged()
        {
            NotifyPropertyChanged("Status");
            NotifyPropertyChanged("IsStatusVisible");
        }
    }
}
