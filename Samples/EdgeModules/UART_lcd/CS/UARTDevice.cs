//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Device;
using EdgeModuleSamples.Common.Logging;
using System;
using System.Threading.Tasks;
using Windows.Devices.SerialCommunication;



namespace UARTLCD
{


    public class UARTDevice : SPBDevice
    {

        public SerialDevice Device { get; private set; }

        private UARTDevice() 
        {
        }
        public async Task<UARTDevice> InitAsync()
        {
            // TODO
            var r = new UARTDevice();
            await Task.CompletedTask;
            return r;
        }
        public static async Task<UARTDevice> CreateUARTDevice(string deviceName)
        {
            Log.WriteLine("finding device {0}", deviceName != null ? deviceName : "(default)");

            Windows.Devices.Enumeration.DeviceInformation info = null;
            if (deviceName != null)
            {
                info = await FindAsync(SerialDevice.GetDeviceSelector(deviceName));
            } else
            {
                info = await FindAsync(SerialDevice.GetDeviceSelector());
            }
            Log.WriteLine("I2c device info {0} null", info == null ? "is" : "is not");

#if TODO
            var settings = new I2cConnectionSettings(DefaultMpuAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
#endif
            var d = new UARTDevice();
            d.Device = await AsAsync(SerialDevice.FromIdAsync(info.Id));
            return d;
        }
        public void Test(TimeSpan duration)
        {
            // TODO
        }

    }
}
