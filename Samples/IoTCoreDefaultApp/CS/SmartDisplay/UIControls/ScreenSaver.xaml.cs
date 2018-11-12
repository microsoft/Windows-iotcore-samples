// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls.ViewModels;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    // The screensaver class triggers automatically after a certain amount of inactivity
    // from keyboard or pointer events.
    // Note: You should only use this screen saver if your device has a pointer or keyboard,
    // or you won't be able to see the app after the screensaver starts.
    //
    // To re-use this screensaver, simply include this class and add the following line
    // to the end of App.OnLaunched:
    // Screensaver.InitializeScreensaver();
    public sealed partial class Screensaver : UserControl
    {
        private ScreensaverVM ViewModel { get; } = new ScreensaverVM();

        public Screensaver()
        {
            InitializeComponent();

            Window.Current.CoreWindow.PointerMoved += App_PointerEvent;
            Window.Current.CoreWindow.KeyDown += App_KeyEvent;
            Window.Current.CoreWindow.PointerPressed += App_PointerEvent;
        }

        private void IsScreensaverEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ViewModel.SetScreensaverEnabled(IsEnabled);
        }

        private void ScreensaverSizeChanged(object sender, SizeChangedEventArgs e)
        {
            ViewModel.SetSize(e.NewSize.Width, e.NewSize.Height);
        }

        private void App_PointerEvent(CoreWindow sender, PointerEventArgs args)
        {
            if (IsEnabled)
            {
                ViewModel.ResetScreensaverTimeout();
            }
        }

        private void App_KeyEvent(CoreWindow sender, KeyEventArgs args)
        {
            if (IsEnabled)
            {
                ViewModel.ResetScreensaverTimeout();
            }
        }
    }
}
