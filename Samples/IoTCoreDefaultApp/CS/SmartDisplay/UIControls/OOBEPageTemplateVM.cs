// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using System.Windows.Input;
using Windows.Foundation.Diagnostics;
using Windows.System.Threading;

namespace SmartDisplay.Controls
{
    public class OOBEPageTemplateVM : BaseViewModel
    {
        #region UI Properties

        public string TitleText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string SubtitleText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public object PanelContent
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }

        public string NextButtonText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string TimeoutText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool IsTimeoutVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsCancelButtonVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public double ProgressValue
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double ProgressSmallChange
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double ProgressMaximum
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Commands

        private RelayCommand _cancelButtonCommand;
        public ICommand CancelButtonCommand
        {
            get
            {
                return _cancelButtonCommand ??
                    (_cancelButtonCommand = new RelayCommand(unused =>
                    {
                        try
                        {
                            CancelCountdown();
                        }
                        catch (Exception ex)
                        {
                            App.LogService.Write(ex.ToString(), LoggingLevel.Error);
                        }
                    }));
            }
        }

        public ICommand NextButtonCommand
        {
            get { return GetStoredProperty<ICommand>(); }
            set { SetStoredProperty(value); }
        }

        #endregion


        // Countdown in seconds before automatically continuing
        protected const int CountDownTimeSeconds = 10;

        // Interval that timer updates the progress bar in milliseconds
        protected const int CountDownIntervalMs = 100;

        protected const int DefaultTimeoutSeconds = 120;

        private ThreadPoolTimer _countdownTimer;
        private ThreadPoolTimer _timer;

        public OOBEPageTemplateVM()
        {
            NextButtonText = Common.GetLocalizedText("NextButton/Content");
            TimeoutText = Common.GetLocalizedText("OOBETimeout/Text");
        }

        /// <summary>
        /// Starts the timer for automatically continuing to the next screen
        /// </summary>
        /// <param name="timeoutSeconds">Seconds before countdown starts, default is 120 seconds</param>
        public void StartTimeoutTimer(int timeoutSeconds = DefaultTimeoutSeconds)
        {
            ProgressMaximum = 100;
            ProgressSmallChange = ProgressMaximum / (CountDownTimeSeconds * 1000 / CountDownIntervalMs);

            _timer = ThreadPoolTimer.CreateTimer((t) =>
            {
                t.Cancel();
                IsTimeoutVisible = true;
                IsCancelButtonVisible = true;
                _countdownTimer = ThreadPoolTimer.CreatePeriodicTimer(CountdownTimerTick, TimeSpan.FromMilliseconds(CountDownIntervalMs));
            }, TimeSpan.FromSeconds(timeoutSeconds));
        }

        private void CountdownTimerTick(ThreadPoolTimer timer)
        {
            var value = ProgressValue + ProgressSmallChange;
            ProgressValue = value;
            if (value >= ProgressMaximum)
            {
                CancelCountdown();
                NextButtonCommand?.Execute(null);
            }
        }

        public void CancelCountdown()
        {
            IsTimeoutVisible = false;
            IsCancelButtonVisible = false;

            _countdownTimer?.Cancel();
            _timer?.Cancel();

            _countdownTimer = null;
            _timer = null;
        }
    }
}
