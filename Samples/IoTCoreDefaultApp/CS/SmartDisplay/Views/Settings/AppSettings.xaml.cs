// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using SmartDisplay.ViewModels.Settings;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    /// <summary>
    /// Contains settings for the app
    /// </summary>
    public sealed partial class AppSettings : PageBase
    {
        public AppSettingsVM ViewModel { get; } = new AppSettingsVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public AppSettings()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            ViewModel.SetUpVM();
            LoadFeatureSettings();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.TearDownVM();
        }

        private void LoadFeatureSettings()
        {
            foreach (var feature in AppComposer.Imports.Features)
            {
                if (feature.SettingsUserControl != null)
                {
                    AppSettingsContainer.Children.Add(CreateSettingPane(feature.SettingsUserControl, feature.FeatureName));
                }
            }
        }

        private StackPanel CreateSettingPane(Type settingsControlType, string headerText)
        {
            // Create pane
            var stackPanel = new StackPanel
            {
                Style = (Style)Application.Current.Resources["SettingPaneStyle"],
                Orientation = Orientation.Vertical
            };

            // Create header
            stackPanel.Children.Add(new TextBlock
            {
                Text = headerText,
                Style = (Style)Application.Current.Resources["SettingHeaderStyle"]
            });

            // Add the settings control
            var settingsControl = (UserControl)Activator.CreateInstance(settingsControlType);
            settingsControl.Width = ViewModel.Width;
            settingsControl.HorizontalAlignment = HorizontalAlignment.Left;
            stackPanel.Children.Add(settingsControl);

            return stackPanel;
        }
    }
}
