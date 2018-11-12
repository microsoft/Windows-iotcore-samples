// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views.Settings;
using System;
using System.Collections.Generic;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Settings page
    /// </summary>
    public sealed partial class SettingsPage : PageBase
    {
        private string _currentPageName;

        // Used for caching the previously navigated settings page
        // This is used instead of NavigationCacheMode because some settings
        // pages need to be refreshed on Navigate
        private static string _previousPageName;

        public static Dictionary<string, Type> SettingsPages = new Dictionary<string, Type>
        {
            { Common.GetLocalizedText("AppPreferences/Text"), typeof(AppSettings) },
            { Common.GetLocalizedText("SystemPreferences/Text"), typeof(SystemSettings) },
            { Common.GetLocalizedText("NetworkPreferences/Text"), typeof(NetworkSettings) },
            { Common.GetLocalizedText("BluetoothPreferences/Text"), typeof(BluetoothSettings) },
            { Common.GetLocalizedText("AppUpdatePreferences/Text"), typeof(AppUpdateSettings) },
            { Common.GetLocalizedText("PowerPreferences/Text"), typeof(PowerSettings) },            
            { Common.GetLocalizedText("DiagnosticSettingsText"), typeof(PrivacySettings) },
            { Common.GetLocalizedText("LocationSettings/Text"), typeof(LocationSettings) },
        };

        public Frame ContentFrame
        {
            get { return SettingsContentFrame; }
        }

        public SettingsPage()
        {
            InitializeComponent();
            DataContext = LanguageManager.GetInstance();
            _currentPageName = Common.GetLocalizedText("AppPreferences/Text");

            BuildSettings();
        }

        // Handle navigation to the current or specific preferences page
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            App.LogService.Write("Navigated to Settings");

            InitializeComponent();

            if (e.Parameter is string pageName)
            {
                _currentPageName = pageName;
            }
            else if (!string.IsNullOrEmpty(_previousPageName))
            {
                _currentPageName = _previousPageName;
            }

            SwitchToSelectedSettings(_currentPageName);
        }

        private void BuildSettings()
        {
            SettingsChoice.Items.Clear();

            foreach (var key in SettingsPages.Keys)
            {
                SettingsChoice.Items.Add(key);
            }
        }

        // Handle ListViewItem being clicked
        private void SettingsChoice_ItemClick(object sender, ItemClickEventArgs e)
        {
            SwitchToSelectedSettings(e.ClickedItem as string);
        }

        // Handle frame switching and UI changes
        private void SwitchToSelectedSettings(string itemName)
        {
            App.LogService.Write("Switching to selected settings: " + itemName);

            if (!SettingsPages.ContainsKey(itemName))
            {
                return;
            }

            Type itemType = SettingsPages[itemName];

            if (itemType != null)
            {
                App.TelemetryService.WriteEvent(itemType.Name + "Navigate");
            }

            if (SettingsContentFrame.CurrentSourcePageType != itemType)
            {
                _currentPageName = itemName;
                SettingsChoice.SelectedItem = itemName;
            }

            _previousPageName = itemName;
            SettingsContentFrame.Navigate(itemType, this);
        }
    }
}
