// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    /// <summary>
    /// This is the base control for creating a new Settings control.  Use this when creating a Settings
    /// control that will be in the side pane and the App Settings page
    /// </summary>
    public class SettingsUserControlBase : UserControl
    {
        // Override this in your derived class.
        protected virtual SettingsBaseViewModel ViewModelImpl => null;
        
        public new double Width
        {
            get { return ViewModelImpl.Width; }
            set { ViewModelImpl.Width = value; }
        }

        public new SolidColorBrush Background
        {
            get { return ViewModelImpl.BackgroundColor; }
            set { ViewModelImpl.BackgroundColor = value; }
        }

        public SettingsUserControlBase() : base()
        {
            Loaded += SettingsUserControlBase_Loaded;
            Unloaded += SettingsUserControlBase_Unloaded;
        }

        private void SettingsUserControlBase_Loaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ViewModelImpl.SetUpVMBase();
        }

        private void SettingsUserControlBase_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ViewModelImpl.TearDownVMBase();
        }
    }
}
