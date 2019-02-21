//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Logging;
using EdgeModuleSamples.Common.MpuDevice;
using ConsoleDotNetCoreSPI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Spi;


namespace ConsoleDotNetCoreSPI
{

    public class SPIMpuDevice : MpuDevice
    {

        static readonly int Mhz20 = 20000;
        public SpiDevice Device { get; private set; }

        private SPIMpuDevice() : base(true)
        {
        }
        public static async Task<SPIMpuDevice> CreateMpuDevice()
        {
            var c = await AsAsync(SpiController.GetDefaultAsync());
            Log.WriteLine("Spi controller {0} null", c == null ? "is" : "is not");
            var settings = new SpiConnectionSettings(DefaultMpuAddress);
            settings.Mode = SpiMode.Mode0;
            settings.ClockFrequency = Mhz20;
            settings.ChipSelectLine = 0;
            var d = new SPIMpuDevice();
            d.Device = c.GetDevice(settings);
            return d;
        }
        public virtual async Task InitAsync()
        {
            await base.InitAsync();
            await Task.Run(() =>
            {
                byte[] disable_i2c = { UserCtrl, 0x10 };
                Write(disable_i2c);
                Thread.Sleep(250);
            });
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
