//
// Copyright (c) Microsoft. All rights reserved.
//

using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Device;
using EdgeModuleSamples.Common.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Spi;


namespace SPIMPU9050
{

    public class SPIMpuDevice : MpuDevice, IDisposable
    {

        static readonly int Mhz20 = 20000;
        public SpiDevice Device { get; private set; }

        private SPIMpuDevice() : base(true)
        {
        }
        ~SPIMpuDevice()
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
                if (Device != null)
                {
                    Device.Dispose();
                    Device = null;
                }
            }
        }

        public static async Task<SPIMpuDevice> CreateMpuDeviceAsync(string deviceName)
        {
            Log.WriteLine("finding device {0}", deviceName != null ? deviceName : "(default)");
            Windows.Devices.Enumeration.DeviceInformation info;
            if (deviceName != null)
            {
                info = await FindAsync(SpiDevice.GetDeviceSelector(deviceName));
            } else
            {
                info = await FindAsync(SpiDevice.GetDeviceSelector());
            }
            Log.WriteLine("Spi device info {0} null", info == null ? "is" : "is not");
            var settings = new SpiConnectionSettings(DefaultMpuAddress);
            settings.Mode = SpiMode.Mode0;
            settings.ClockFrequency = Mhz20;
            settings.ChipSelectLine = 0;
            var d = new SPIMpuDevice();
            d.Device = await AsAsync(SpiDevice.FromIdAsync(info.Id, settings));
            return d;
        }
        public override async Task InitAsync()
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
        public override Int16 GetMpuValue(byte msbReg, byte lsbReg)
        {
            byte[] CMD = { msbReg, lsbReg, 0 };
            Device.TransferFullDuplex(CMD, CMD);
            return (Int16)(((UInt16)(CMD[1] << 8)) | CMD[2]);
        }

    }
}
