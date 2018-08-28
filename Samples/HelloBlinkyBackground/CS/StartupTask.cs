// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using Windows.Devices.Gpio;
using Windows.System.Threading;
using System.Diagnostics;

namespace BlinkyHeadlessCS
{
    public sealed class StartupTask : IBackgroundTask
    {
        BackgroundTaskDeferral deferral;
        private GpioPinValue value = GpioPinValue.High;
        private const int LED_PIN = 5;
        private GpioPin pin;
        private ThreadPoolTimer timer;
        IBackgroundTaskInstance _taskInstance = null;

        BackgroundTaskCancellationReason _cancelReason = BackgroundTaskCancellationReason.Abort;
        volatile bool _cancelRequested = false;

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Background " + taskInstance.Task.Name + " Starting...");
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            deferral = taskInstance.GetDeferral();
            _taskInstance = taskInstance;

            InitGPIO();
            timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(500));
            
        }
        private void InitGPIO()
        {
            pin = GpioController.GetDefault().OpenPin(LED_PIN);
            pin.Write(GpioPinValue.High);
            pin.SetDriveMode(GpioPinDriveMode.Output);
        }

        private void Timer_Tick(ThreadPoolTimer timer)
        {
            if (_cancelRequested == false)
            {
                value = (value == GpioPinValue.High) ? GpioPinValue.Low : GpioPinValue.High;
                pin.Write(value);
            } else
            {
                timer.Cancel();
                //
                // Indicate that the background task has completed.
                //
                deferral.Complete();
            }
            
        }

        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            //
            // Indicate that the background task is canceled.
            //
            _cancelRequested = true;
            _cancelReason = reason;

            Debug.WriteLine("Background " + sender.Task.Name + " Cancel Requested...");
        }

    }
}
