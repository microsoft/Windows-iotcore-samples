//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Logging;
using EdgeModuleSamples.Common.MpuDevice;
using ConsoleDotNetCoreI2c;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.I2c;



namespace ConsoleDotNetCoreI2c
{


    public class I2CMpuDevice : MpuDevice
    {

        public I2cDevice Device { get; private set; }

        private I2CMpuDevice() : base(false)
        {
        }
        public static async Task<I2CMpuDevice> CreateMpuDevice()
        {
            var c = await AsAsync(I2cController.GetDefaultAsync());
            Log.WriteLine("I2c controller {0} null", c == null ? "is" : "is not");
            var settings = new I2cConnectionSettings(DefaultMpuAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            var d = new I2CMpuDevice();
            d.Device = c.GetDevice(settings);
            return d;
        }
        public override void Write(byte[] val)
        {
            Device.Write(val);
        }
        public override void Read(byte[] val)
        {
            Device.Read(val);
        }

    }
}
