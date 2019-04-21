//
// Copyright (c) Microsoft. All rights reserved.
//

using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Device;
using EdgeModuleSamples.Common.Logging;
using PWMFruit;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Pwm;


namespace PWMFruit
{


    public class PWMDevice : SPBDevice, IDisposable
    {
        public PwmController Device { get; private set; }
        public PwmPin Pin { get; private set; }

        float _currentSpeed = 0.0f;
        bool _started = false;
        private PWMDevice()
        {
        }
        ~PWMDevice()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Pin != null)
                {
                    Pin.Dispose();
                    Pin = null;
                }
            }
        }

        public static async Task ListPWMDevicesAsync()
        {
            var info = await FindAsync(PwmController.GetDeviceSelector());
        }
        public static async Task<PWMDevice> CreatePWMDeviceAsync(string deviceName, int pinNumber)
        {
            Log.WriteLine("finding device {0}", deviceName != null ? deviceName : "(default)");

            Windows.Devices.Enumeration.DeviceInformation info = null;
            if (deviceName != null)
            {
                info = await FindAsync(PwmController.GetDeviceSelector(deviceName));
            }
            else
            {
                info = await FindAsync(PwmController.GetDeviceSelector());
            }
            Log.WriteLine("PWM device info {0} null", info == null ? "is" : "is not");

            var d = new PWMDevice();
            d.Device = await AsAsync(PwmController.FromIdAsync(info.Id));
            if (d.Device != null)
            {
                Log.WriteLine("Pin Count {0}", d.Device.PinCount);
            }
            d.Pin = d.Device.OpenPin(pinNumber);
            return d;
        }

        public void SetSpeed(int pct)
        {
            float val = pct * 1.0f / 100.0f;
            Log.WriteLine("SetSpeed(int) val {0}", val);
            if (val > 0.01f && _currentSpeed <= 0.01f)
            {
                Log.WriteLineVerbose("Inertial boost");
                SetSpeed(1.0f);  // full blast for a moment to overcome inertia and get the motor shaft turning
                Thread.Sleep(100);
            }
            SetSpeed(val);
        }
        // note: empirically my motor will only run at minimum 45%.  likely this will need to be tuned for each motor
        readonly float MINIMUM_DUTY_CYCLE = 0.45f;
        public void SetSpeed(float pct)
        {
            if (pct <= 0.01f)
            {
                pct = 0.0f;
            } else if (pct > 1.0f)
            {
                pct = 1.0f;
            } else
            {
                if (pct < MINIMUM_DUTY_CYCLE)
                {
                    pct = MINIMUM_DUTY_CYCLE;
                } else
                {
                    // scale 0-100 to min-100
                    float val = (1.0f - MINIMUM_DUTY_CYCLE) * pct + MINIMUM_DUTY_CYCLE;
                }
            }
            Log.WriteLine("SetSpeed(float) final clipped pct {0}", pct);
            set(pct);
        }
        private void set(float val)
        {
            if (Math.Abs(_currentSpeed - val) < 0.01f)
            {
                Log.WriteLine("current speed already {0} -- ignoring", val);
                return;
            }
            if (val < 0.01f)
            {
                Log.WriteLine("speed 0 Stopping Pin");
                Pin.Stop();
                _started = false;
                _currentSpeed = 0.0f;
                return;
            }
            if (!_started)
            {
                Log.WriteLine("starting pin");
                Device.SetDesiredFrequency((Device.MinFrequency + Device.MaxFrequency) / 2);
                Pin.Polarity = PwmPulsePolarity.ActiveHigh;
                Pin.Start();
                _started = true;
            }
            Pin.SetActiveDutyCyclePercentage(val);
            _currentSpeed = val;
            Log.WriteLine("duty cycle {0} at freq {1}", val, Device.ActualFrequency);
        }

        public void Test(TimeSpan testDuration, int pct)
        {
            Log.WriteLine("Test started");
            var t = DateTime.Now;
            Device.SetDesiredFrequency((Device.MinFrequency + Device.MaxFrequency) / 2);
            Pin.Polarity = PwmPulsePolarity.ActiveHigh;
            Pin.Start();
            var dc = (pct * 1.0) / 100.0;
            Pin.SetActiveDutyCyclePercentage(dc);
            Log.WriteLine("duty cycle {0} at freq {1}", dc, Device.ActualFrequency);
            while (DateTime.Now - t < testDuration)
            {
            }
            Log.WriteLine("Test complete");
            Environment.Exit(3);
        }

    }
}
