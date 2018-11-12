// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    public sealed partial class LoginPopupControl : UserControl
    {
        public bool IsOpen
        {
            get { return PopupElement.IsOpen; }
            set { PopupElement.IsOpen = value; }
        }

        private double WindowWidth => Window.Current.Bounds.Width;
        private double WindowHeight => Window.Current.Bounds.Height;

        private const double ViewboxMaxWidth = 800;

        public LoginPopupControl()
        {
            InitializeComponent();

            IsOpen = false;

            BorderElement.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215));
            BorderElement.BorderThickness = new Thickness(2);
            BorderElement.Background = new SolidColorBrush(Color.FromArgb(255, 60, 60, 60));
        }

        ~LoginPopupControl()
        {
            WdpLoginControl.Dispose();
        }

        private static LoginPopupControl _instance = null;
        public static async Task<bool> SignInAsync(string description = null, bool showCancelButton = true)
        {
            if (_instance == null)
            {
                _instance = new LoginPopupControl();
            }

            return await _instance.DoSignInAsync(description, showCancelButton);
        }

        public async Task<bool> DoSignInAsync(string description = null, bool showCancelButton = true)
        {
            // Return if valid WDP credentials are available
            if (await DevicePortalUtil.IsSignedInAsync())
            {
                return true;
            }

            // Set the size of the popup box
            ViewboxElement.Width = Math.Min(0.75 * WindowWidth, ViewboxMaxWidth);
            ViewboxElement.Height = 0.5 * ViewboxElement.Width;

            // Reset and customize the WDP control
            WdpLoginControl.Description = description;
            WdpLoginControl.ShowCancelButton = showCancelButton;
            WdpLoginControl.Reset();

            // Set up a TaskCompletionSource to handle the event and complete the function
            var tcs = new TaskCompletionSource<bool>();
            TypedEventHandler<object, SignInCompletedArgs> signInCompletedHandler = null;
            signInCompletedHandler = (s, e) =>
            {
                if (e.IsUserInitiated || e.IsSuccessful)
                {
                    tcs.TrySetResult(e.IsSuccessful);
                }
            };
            WdpLoginControl.SignInCompleted += signInCompletedHandler;
            
            // Set position of the popup
            PopupElement.HorizontalOffset = (WindowWidth / 2) - (ViewboxElement.Width / 2);
            PopupElement.VerticalOffset = (WindowHeight / 2) - (ViewboxElement.Height / 2);

            // Show popup
            PopupElement.IsOpen = true;

            // Wait for the result
            var result = await tcs.Task;
            PopupElement.IsOpen = false;

            // Remove event handlers
            WdpLoginControl.SignInCompleted -= signInCompletedHandler;

            return result;
        }
    }
}
