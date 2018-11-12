// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts.Interfaces;
using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Input;

namespace SmartDisplay.Controls
{
    public sealed partial class NotificationControl : INotificationControl
    {
        public new double Width
        {
            get { return ViewModel.Width; }
            set { ViewModel.Width = value; }
        }

        public new double Height
        {
            get { return ViewModel.Height; }
            set { ViewModel.Height = value; }
        }

        public string Text
        {
            get { return ViewModel.Text; }
            set { ViewModel.Text = value; }
        }

        public string Icon
        {
            get { return ViewModel.Icon; }
            set { ViewModel.Icon = value; }
        }

        public event EventHandler NotificationPressed;
        private NotificationControlVM ViewModel { get; } = new NotificationControlVM();
        private DispatcherTimer _hideTimer;
        private const int DefaultTimeoutMs = 2000;

        public NotificationControl()
        {
            InitializeComponent();

            ViewModel.Text = string.Empty;
            ViewModel.IsVisible = false;
            
            _hideTimer = new DispatcherTimer();
            _hideTimer.Tick += (s, e) =>
            {
                _hideTimer.Stop();
                Hide();
            };
        }

        public void Show(string text, int timeoutMs = DefaultTimeoutMs, string icon = "⚪")
        {
            ViewModel.Text = text;
            ViewModel.Icon = icon;
            ViewModel.IsVisible = true;

            _notification.IsOpen = true;

            // Restart hide timer
            _hideTimer.Interval = TimeSpan.FromMilliseconds(timeoutMs);
            _hideTimer.Start();
        }

        public void Hide()
        {
            _notification.IsOpen = false;
        }

        private void Notification_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            NotificationPressed?.Invoke(this, EventArgs.Empty);
        }

        // Close button pressed
        private void CloseButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
            Hide();
        }
    }
}
