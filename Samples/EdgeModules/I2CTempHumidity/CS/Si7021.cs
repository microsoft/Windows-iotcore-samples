//
// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.I2c;

using EdgeModuleSamples.Common.Logging;
using static EdgeModuleSamples.Common.AsyncHelper;

namespace EdgeModuleSamples.Devices
{
    class Si7021: IDisposable
    {
        #region Public Interface
        public static async Task<Si7021> Open()
        {
            var result = new Si7021();
            var i2cSettings = new I2cConnectionSettings(I2C_ADDRESS);
            var controller = await AsAsync(I2cController.GetDefaultAsync());

            if (null == controller)
                throw new ApplicationException("No I2C controller found on this machine.");

            result.Device = controller.GetDevice(i2cSettings);
            result.Initialize();

            return result;
        }

        // Call to update the temperature and humidity values
        public void Update()
        {
            var humidityreading = I2CReadWrite(new byte[] { CMD_MEASURE_RH_HOLD }, 3 );
            UInt16 msb = (UInt16)(humidityreading[0]);
            UInt16 lsb = (UInt16)(humidityreading[1]);
            UInt16 humidity16 = (UInt16)((msb << 8) | lsb);
            Humidity = ((double)humidity16 * 125.0) / 65536.0 - 6.0;

            var tempreading = I2CReadWrite(new byte[] { CMD_READ_TEMP }, 2 );
            msb = (UInt16)(tempreading[0]);
            lsb = (UInt16)(tempreading[1]);
            UInt16 temp16 = (UInt16)((msb << 8) | lsb);
            Temperature = ((double)temp16 * 175.72 / 65536.0) - 46.85;
        }

        public double Temperature { get; private set; }

        public double Humidity { get; private set; }

        public string Model { get; private set; }

        public string SerialNumber { get; private set; }

        public string FirmwareRevision { get; private set; }

        #endregion

        #region Internals

        // Do not call direcrtly, use Open()
        protected Si7021()
        {
        }

        // Call once after device is set
        private void Initialize()
        {
            // Device serial number

            var serialA = I2CReadWrite(new byte[] { CMD_READ_ID1_1, CMD_READ_ID1_2 }, 8 );
            var serialB = I2CReadWrite(new byte[] { CMD_READ_ID2_1, CMD_READ_ID2_2 }, 6 );
            var firmwarerev = I2CReadWrite(new byte[] { CMD_READ_FIRMWARE_VER_1, CMD_READ_FIRMWARE_VER_2 }, 1 );

            var serialnumberbytes = new byte[] { serialA[0], serialA[2], serialA[4], serialA[6], serialB[0], serialB[1], serialB[3], serialB[4] };
            SerialNumber = serialnumberbytes.Aggregate(new StringBuilder(),(sb,b)=>sb.Append(b.ToString("X"))).ToString();

            Model = $"Si70{serialB[0]}";
            if (firmwarerev[0] == 0xff)
                FirmwareRevision = "1.0";
            else if (firmwarerev[0] == 0x20)
                FirmwareRevision = "2.0";
        }

        private byte[] I2CReadWrite(byte[] writeBuffer, int readsize)
        {
            byte[] readBuffer = new byte[readsize];
            var result = Device.WriteReadPartial(writeBuffer,readBuffer);
            Log.WriteLineVerbose(result.ToString());

            var display = readBuffer.Aggregate(new StringBuilder(),(sb,b)=>sb.Append(string.Format("{0:X2} ",b)));
            Log.WriteLineVerbose(display.ToString());

            return readBuffer;
        }

        private const byte I2C_ADDRESS = 0x40;
        private I2cDevice Device;

        #endregion

        #region I2C Commands

        // Read previous T data from RH measurement command
        const byte CMD_READ_TEMP = 0xE0;

        // Perform RH (and T) measurement.
        const byte CMD_MEASURE_RH_HOLD = 0xE5;

        // Read electronic ID
        const byte CMD_READ_ID1_1 = 0xFA;
        const byte CMD_READ_ID1_2 = 0x0F;
        const byte CMD_READ_ID2_1 = 0xFC;
        const byte CMD_READ_ID2_2 = 0xc9;

        // Read firmware revision
        const byte CMD_READ_FIRMWARE_VER_1 = 0x84;
        const byte CMD_READ_FIRMWARE_VER_2 = 0xB8;

        #endregion

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Device.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~DS3231() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
