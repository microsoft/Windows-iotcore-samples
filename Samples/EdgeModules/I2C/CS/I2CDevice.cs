//
// Copyright (c) Microsoft. All rights reserved.
//

using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Logging;
using ConsoleDotNetCoreI2c;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;


namespace ConsoleDotNetCoreI2c
{


    public class I2cDevice
    {
        public I2cController Device { get; private set; }

        private I2cDevice()
        {
        }
        public static async Task<I2cDevice> CreateI2cDevice()
        {
            var d = new I2cDevice();
            d.Device = await AsAsync(I2cController.GetDefaultAsync());
            Log.WriteLine("I2c Device ctor complete.  controller {0} null", d.Device == null ? "is" : "is not");
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
