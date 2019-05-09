//
// Copyright (c) Microsoft. All rights reserved.
//

using static EdgeModuleSamples.Common.AsyncHelper;
using EdgeModuleSamples.Common.Logging;
using System;
using System.Threading;
using System.Collections.Generic;
using System.Threading.Tasks;
// do not use anything out of Universal API contract except Windows.Devices.Enumeration and the portion of Windows.Storage.Streams needed for serial
using Windows.Devices.Enumeration;




namespace EdgeModuleSamples.Common.Device
{

    public abstract class SPBDevice
    {
        public static readonly string friendlyNameProperty = "System.DeviceInterface.Spb.ControllerFriendlyName";

        protected static async Task<DeviceInformation> FindAsync(string selector) {
            List<string> properties = new List<string>();
            properties.Add(friendlyNameProperty);
            var devices = await AsAsync(DeviceInformation.FindAllAsync(
                    selector,
                    properties));
            if (devices.Count < 1)
            {
                throw new ApplicationException(string.Format("No device found by {0}", selector));
            }

            Console.WriteLine("Device Info from {0}", selector);
            Console.WriteLine("FriendlyName DeviceId");
            DeviceInformation r = null; // return the first one but log if there's duplicates
            foreach (var i in devices)
            {
                if (r == null)
                {
                    r = i;
                }
                object o;
                i.Properties.TryGetValue(friendlyNameProperty, out o);
                string friendlyName = (string)o;
                var Id = i.Id;
                Console.WriteLine("  {0} {1}", friendlyName, Id);
            }
            return r;
        }
    }

    public abstract class MpuDevice : SPBDevice
    {
        public readonly static int DEFAULT_MPU_DEVICE_RETRY_COUNT = 10;
        // NOTE: this sample is only using the Z-axis accelerometer for orientation detection
        // if you wanted to do something more complex see the 6050/9050 datasheet for additional register definitions
        protected static readonly byte DefaultMpuAddress = 0x68;
        // registers for InvenSense Mpu-6050  

        protected static readonly byte RateConfiguration = 0x1a;
        protected static readonly byte AccelerometerZmsb = 0x3f;
        protected static readonly byte AccelerometerZlsb = 0x40;
        protected static readonly byte UserCtrl = 0x6a;
        protected static readonly byte PowerManagement = 0x6b;

        protected static readonly byte SPIReadMask = 0x80;
        private byte SpiRead = 0;
        private bool _useSpi = false;

        public abstract Int16 GetMpuValue(byte msbReg, byte lsbReg);
        public abstract void Write(byte[] val);
        public abstract void Read(byte[] val);

        public class OrientationEventArgs : EventArgs
        {
            public Orientation newOrientation;
        }
        public delegate void OrientationEventHandler(MpuDevice device, OrientationEventArgs args);
        public event OrientationEventHandler OrientationChanged;

        private object _orientationLock = new object();
        private Orientation? _orientation = Orientation.RightSideUp;
        public Orientation CurrentOrientation
        {
            get
            {
                lock (_orientationLock)
                {
                    return _orientation.Value;
                }
            }
            private set
            {
                lock (_orientationLock)
                {
                    if (!_orientation.HasValue || _orientation.Value != value)
                    {
                        _orientation = value;
                        OrientationEventArgs e = new OrientationEventArgs();
                        e.newOrientation = _orientation.Value;
                        OrientationChanged?.Invoke(this, e);
                    }
                }
            }
        }
        protected MpuDevice(bool useSpi)
        {
            if (useSpi)
            {
                _useSpi = useSpi;
                SpiRead = SPIReadMask;
            }
        }
        public virtual async Task InitAsync()
        {
            await Task.Run(() =>
            {
                byte[] reset = { PowerManagement, 0x80 };
                Write(reset);
                Thread.Sleep(250);
                byte[] awake = { PowerManagement, 0 };
                Write(awake);
                Thread.Sleep(250);
            });
            return;
        }
        private byte ReadReg(byte reg)
        {
            return (byte)(reg | SpiRead);
        }
        public Int16 CurrentAccelerometerZ
        {
            get
            {
                // by default, accel on this chip measure +-/2G and return +/-16384
                return GetMpuValue(ReadReg(AccelerometerZmsb), ReadReg(AccelerometerZlsb));
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
                // notify initial state without worrying about bounce
                CurrentOrientation = OrientationFromAccelerometer(CurrentAccelerometerZ);
                Log.WriteLine("set initial orientation to {0}", CurrentOrientation);
                int retry_count = 0;
                while (!token.IsCancellationRequested) {
                    try
                    {
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
                        retry_count = 0;
                        Thread.Sleep((int)(DebounceInterval.TotalMilliseconds / 4));
                    } catch (System.IO.FileNotFoundException e)
                    {
                        Log.WriteLineVerbose("MPU I/O exception {0}", e.ToString());
                        if (++retry_count > DEFAULT_MPU_DEVICE_RETRY_COUNT)
                        {
                            Log.WriteLineError("MPU I/O exception retry count exceeded. last exception {0}", e.ToString());
                            Environment.Exit(3);
                        }
                        else
                        {
                            Log.WriteLineVerbose("retrying");
                        }

                    }
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
