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
        public PwmController Device { get; set; }

        private PWMDevice()
        {
        }
        public static async Task ListPWMDevicesAsync()
        {
            var info = await FindAsync(PwmController.GetDeviceSelector());
        }
        public static async Task<PWMDevice> CreatePWMDeviceAsync(string deviceName)
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
            return d;
        }


        public void Test(TimeSpan testDuration, TimeSpan pinInterval)
        {
            Log.WriteLine("Test started");
            var t = DateTime.Now;
            while (DateTime.Now - t < testDuration)
            {
            }
        }

    }
}
