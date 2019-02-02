//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
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


    public class MpuDevice
    {
        // NOTE: this sample is only using the Z-axis accelerometer for orientation detection
        // if you wanted to do something more complex see the 6050 datasheet for additional register definitions
        static readonly byte DefaultMpuAddress = 0x68;
        // registers for InvenSense Mpu-6050  
        static readonly byte PowerManagement = 0x6b;
        static readonly byte RateConfiguration = 0x1a;
        static readonly byte AccelerometerZmsb = 0x3f;
        static readonly byte AccelerometerZlsb = 0x40;

        public I2cDevice Device { get; private set; }

        public class OrientationEventArgs : EventArgs
        {
            public Orientation newOrientation;
        }
        public delegate void OrientationEventHandler(MpuDevice device, OrientationEventArgs args);
        public event OrientationEventHandler OrientationChanged;

        private Object _orientationLock = new object();
        private Orientation _orientation = Orientation.RightSideUp;
        public Orientation CurrentOrientation
        {
            get
            {
                lock (_orientationLock)
                {
                    return _orientation;
                }
            }
            private set
            {
                lock (_orientationLock)
                {
                    if (_orientation != value)
                    {
                        _orientation = value;
                        OrientationEventArgs e = new OrientationEventArgs();
                        e.newOrientation = _orientation;
                        OrientationChanged?.Invoke(this, e);
                    }
                }
            }
        }
        private MpuDevice()
        {
        }
        public static async Task<MpuDevice> CreateMpuDevice()
        {
            var c = await AsAsync(I2cController.GetDefaultAsync());
            Log.WriteLine("I2c controller {0} null", c == null ? "is" : "is not");
            var settings = new I2cConnectionSettings(DefaultMpuAddress);
            settings.BusSpeed = I2cBusSpeed.FastMode;
            var d = new MpuDevice();
            d.Device = c.GetDevice(settings);
            return d;
        }
        public async Task InitAsync()
        {
            await Task.Run(() =>
            {
                byte[] reset = { PowerManagement, 0x80 };
                Device.Write(reset);
                Thread.Sleep(250);
                byte[] awake = { PowerManagement, 0 };
                Device.Write(awake);
                byte configval = (2 << 3) & 1; // gyro x dlfp == 1 /TODO: s/b |
                byte[] rateconfig = { RateConfiguration, configval };
                Device.Write(rateconfig);
                rateconfig[1] = (7 << 3) & 1; // accel z dlfp == 1 /TODO: s/b |
                Device.Write(rateconfig);
                Thread.Sleep(250);
            });
            return;
        }
        private Int16 GetMpuValue(byte msbReg, byte lsbReg)
        {
            byte[] msbCMD = { msbReg };
            Device.Write(msbCMD);
            byte[] HighVal = { 0 };
            Device.Read(HighVal);
            byte[] lsbCMD = { lsbReg };
            Device.Write(lsbCMD);
            byte[] LowVal = { 0 };
            Device.Read(LowVal);
            return (Int16)(((UInt16)(HighVal[0] << 8)) | LowVal[0]);
        }
        public Int16 CurrentAccelerometerZ
        {
            get
            {
                // by default, accel on this chip measure +-/2G and return +/-16384
                return GetMpuValue(AccelerometerZmsb, AccelerometerZlsb);
            }
        }
        public CancellationTokenSource Monitoring { get; private set; }
        private static readonly TimeSpan DebounceInterval = TimeSpan.FromMilliseconds(100);
        private Orientation OrientationFromAccelerometer(Int16 accelerometerVal)
        {
            if (accelerometerVal > 0)
            {
                return Orientation.RightSideUp;
            } else
            {
                return Orientation.UpsideDown;
            }
        }
        public async Task BeginOrientationMonitoringAsync()
        {
            if (Monitoring != null)
            {
                throw new ApplicationException("only 1 monitoring task can exist at one time");
            }
            Monitoring = new CancellationTokenSource();
            await Task.Run(() =>
            {
                var token = Monitoring.Token;
                DateTime changeTime = DateTime.Now;
                bool changing = false;
                while (!token.IsCancellationRequested) {
                    var z = CurrentAccelerometerZ;
                    var newOrientation = OrientationFromAccelerometer(z);
                    var cur = CurrentOrientation;
                    Log.WriteLineVerbose("cur = {0} Z = {1}, new = {2} c = {3}", cur, z, newOrientation, changing);
                    DateTime currentFetch = DateTime.Now;
                    TimeSpan delta = currentFetch - changeTime;
                    if (newOrientation != cur)
                    {
                        if (!changing)
                        {
                            Log.WriteLineVerbose("Changing");
                            changing = true;
                            changeTime = currentFetch;
                        }
                        else
                        {
                            if (delta > DebounceInterval)
                            {
                                Log.WriteLine("Completing Change from {0} to {1} after {2}", cur, newOrientation, delta.TotalMilliseconds);
                                CurrentOrientation = newOrientation;
                                changing = false;
                                changeTime = currentFetch;
                            }
                            else
                            {
                                Log.WriteLineVerbose("Settling for {0}", delta.TotalMilliseconds);
                            }
                        }
                    }
                    else
                    {
                        if (changing)
                        {
                            if (delta > DebounceInterval)
                            {
                                Log.WriteLineVerbose("resetting bounce after {0}");
                                changing = false;
                                changeTime = currentFetch;
                            }
                            else
                            {
                                Log.WriteLineVerbose("settling for {0}", delta.TotalMilliseconds);
                            }
                        }
                        else
                        {
                            Log.WriteLineVerbose("no change for {0}", delta.TotalMilliseconds);
                        }
                    }
                Thread.Sleep((int)(DebounceInterval.TotalMilliseconds / 4));
                }
                Monitoring = null;
            });
            return;
        }
        void EndOrientationMonitoring()
        {
            Monitoring.Cancel();
        }

        public void Test(TimeSpan testDuration)
        {
            Log.WriteLine("Test requested");
            Task.Run(async () =>
            {
                Log.WriteLine("Test started");
                await BeginOrientationMonitoringAsync();
                Log.WriteLine("ending test lambda");
            });
            Thread.Sleep(testDuration);
            Log.WriteLine("ending test");
            EndOrientationMonitoring();
            Log.WriteLine("Test Complete");
        }

    }
}
