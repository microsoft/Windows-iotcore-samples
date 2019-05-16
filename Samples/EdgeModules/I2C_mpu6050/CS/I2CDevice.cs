//
// Copyright (c) Microsoft. All rights reserved.
//

using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Device;
using EdgeModuleSamples.Common.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.I2c;



namespace I2CMPU6050
{


    public class I2CMpuDevice : MpuDevice, IDisposable
    {

        public I2cDevice Device { get; private set; }

        private I2CMpuDevice() : base(false)
        {
        }
        ~I2CMpuDevice()
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

        public static async Task<I2CMpuDevice> CreateMpuDeviceAsync(string deviceName)
        {
            Log.WriteLine("finding device {0}", deviceName != null ? deviceName : "(default)");

            Windows.Devices.Enumeration.DeviceInformation info = null;
            if (deviceName != null)
            {
                info = await FindAsync(I2cDevice.GetDeviceSelector(deviceName));
            } else
            {
                info = await FindAsync(I2cDevice.GetDeviceSelector());
            }
            Log.WriteLine("I2c device info {0} null", info == null ? "is" : "is not");

            var settings = new I2cConnectionSettings(DefaultMpuAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            var d = new I2CMpuDevice();
            d.Device = await AsAsync(I2cDevice.FromIdAsync(info.Id, settings));
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
        public override Int16 GetMpuValue(byte msbReg, byte lsbReg)
        {
            byte[] msbCMD = { msbReg };
            Write(msbCMD);
            byte[] HighVal = { 0 };
            Read(HighVal);
            byte[] lsbCMD = { lsbReg };
            Write(lsbCMD);
            byte[] LowVal = { 0 };
            Read(LowVal);
            return (Int16)(((UInt16)(HighVal[0] << 8)) | LowVal[0]);
        }

    }
}
