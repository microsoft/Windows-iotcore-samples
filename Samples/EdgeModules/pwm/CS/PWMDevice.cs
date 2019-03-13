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


    public class PWMDevice : SPBDevice
    {
        public PwmController Device { get; private set; }
        public PwmPin Pin { get; private set; } 

        private PWMDevice()
        {
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
