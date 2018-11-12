// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public partial class DevicePortalLoginControl : UserControl, IDisposable
    {
        #region Design time properties

        public event TypedEventHandler<object, SignInCompletedArgs> SignInCompleted;

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(DevicePortalLoginControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (DevicePortalLoginControl)sender;
                    control.ViewModel.Title = (args.NewValue as string) ?? string.Empty;
                })
            );

        public object Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description",
                typeof(string),
                typeof(DevicePortalLoginControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (DevicePortalLoginControl)sender;
                    control.ViewModel.Description = (args.NewValue as string) ?? string.Empty;
                })
            );

        public object Description
        {
            get { return GetValue(DescriptionProperty); }
            set { SetValue(DescriptionProperty, value); }
        }

        public static readonly DependencyProperty ShowCancelButtonProperty =
            DependencyProperty.Register("ShowCancelButton",
                typeof(bool),
                typeof(DevicePortalLoginControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (DevicePortalLoginControl)sender;
                    control.ViewModel.IsCancelVisible = (args.NewValue as bool?) ?? false;
                })
            );

        public bool ShowCancelButton
        {
            get { return (GetValue(ShowCancelButtonProperty) as bool?) ?? false; }
            set { SetValue(ShowCancelButtonProperty, value); }
        }
        
        #endregion

        private DevicePortalLoginControlVM ViewModel { get; } = new DevicePortalLoginControlVM();
        
        public DevicePortalLoginControl()
        {
            InitializeComponent();
            Loaded += DevicePortalLoginControl_Loaded;
            Unloaded += DevicePortalLoginControl_Unloaded;
            ViewModel.SignInCompleted += (s, e) => SignInCompleted?.Invoke(s, e);
            ViewModel.IsCancelVisible = ShowCancelButton;
        }

        public void Reset() => ViewModel.Reset();

        private async void DevicePortalLoginControl_Loaded(object sender, RoutedEventArgs e)
        {
            await ViewModel.SignInAsync();
        }

        private void DevicePortalLoginControl_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.TearDownVM();
        }

        #region IDisposable
        bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    ViewModel.Dispose();
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~DevicePortalLoginControl()
        {
            Dispose(false);
        }
        #endregion
    }

    public class SignInCompletedArgs : EventArgs
    {
        public bool IsSuccessful;
        public bool IsUserInitiated;
    }
}
