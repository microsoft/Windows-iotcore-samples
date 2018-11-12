// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using Windows.System.Threading;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Controls.ViewModels
{
    public class ScreensaverVM : BaseViewModel
    {
        public int ImageWidth
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }

        public int ImageHeight
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }

        public BitmapImage ImageSource
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public double LeftImagePosition
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double TopImagePosition
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsScreensaverVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsScreensaverEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsImageVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public double PageWidth
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double PageHeight
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        private ThreadPoolTimer _timeoutTimer;
        private ThreadPoolTimer _moveTimer;

        private readonly Random _randomizer = new Random();

        public ScreensaverVM()
        {
            // Set screen saver to activate after 1 minute
            _timeoutTimer = ThreadPoolTimer.CreateTimer(TimeoutTimer_Tick, TimeSpan.FromMinutes(1));
            ImageSource = new BitmapImage(DeviceInfoPresenter.GetBoardImageUri());
            ImageWidth = 200;
            ImageHeight = 200;
            IsImageVisible = true;
        }

        public void SetScreensaverEnabled(bool isEnabled)
        {
            IsScreensaverEnabled = isEnabled;
            // If you disable the screensaver in one window, hide any active screensavers in other windows
            IsScreensaverVisible = isEnabled ? IsScreensaverVisible : false;
            _timeoutTimer?.Cancel();
            if (isEnabled)
            {
                _timeoutTimer = ThreadPoolTimer.CreateTimer(TimeoutTimer_Tick, TimeSpan.FromMinutes(1));
            }
        }

        public void SetSize(double width, double height)
        {
            PageWidth = width;
            PageHeight = height;
        }

        private void MoveTimer_Tick(ThreadPoolTimer timer)
        {
            _moveTimer?.Cancel();
            if (!IsScreensaverEnabled)
            {
                return;
            }

            IsImageVisible = false;
            LeftImagePosition = _randomizer.NextDouble() * (PageWidth - ImageWidth);
            TopImagePosition = _randomizer.NextDouble() * (PageHeight - ImageHeight);
            IsImageVisible = true;

            _moveTimer = ThreadPoolTimer.CreateTimer(MoveTimer_Tick, TimeSpan.FromSeconds(5));
        }

        // Triggered when there hasn't been any key or pointer events in a while
        private void TimeoutTimer_Tick(ThreadPoolTimer timer)
        {
            _timeoutTimer?.Cancel();
            _moveTimer?.Cancel();

            if (!IsScreensaverEnabled)
            {
                return;
            }
            IsScreensaverVisible = true;

            _moveTimer = ThreadPoolTimer.CreateTimer(MoveTimer_Tick, TimeSpan.FromSeconds(5));
        }

        // Resets the timer and starts over.
        public void ResetScreensaverTimeout()
        {
            _moveTimer?.Cancel();
            _timeoutTimer?.Cancel();
            _timeoutTimer = ThreadPoolTimer.CreateTimer(TimeoutTimer_Tick, TimeSpan.FromMinutes(1));
            IsScreensaverVisible = false;
        }
    }
}
