namespace SampleModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Linq;
    using Microsoft.Azure.Devices.Client;
    using Newtonsoft.Json;
    using Windows.Devices.Enumeration;
    using Windows.Devices.Gpio;
    using Windows.Devices.I2c;

    using EdgeModuleSamples.Common;
    using static EdgeModuleSamples.Common.AsyncHelper;

    class Program
    {
        static AppOptions Options;
        static ModuleClient ioTHubModuleClient;
        static readonly Random Rnd = new Random();

        static async Task Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);

                //
                // Enumerate devices
                //

                var devicepaths = await AsAsync(DeviceInformation.FindAllAsync(I2cDevice.GetDeviceSelector(), new[] { "System.DeviceInterface.Spb.ControllerFriendlyName" }));
                if (Options.ShowList || string.IsNullOrEmpty(Options.DeviceId))
                {
                    if (devicepaths?.FirstOrDefault() != null)
                    {
                        Log.WriteLine("Available devices:");

                        foreach (var device in devicepaths)
                        {
                            Console.WriteLine(device.Id);
                        }
                    }
                    else
                    {
                        Log.WriteLine("There are no I2C controllers on this system");
                    }
                    return;
                }

                //
                // Open Device
                //

                string aqs = string.Empty;
                aqs = I2cDevice.GetDeviceSelector();

#if nope
                string aqs;
                if (string.IsNullOrEmpty(Options.DeviceId))
                    aqs = I2cDevice.GetDeviceSelector();
                else
                {
                    var devicepath = devicepaths.Where(x=>x.Id.ToLowerInvariant().Contains(Options.DeviceId)).FirstOrDefault();

                    if (null == devicepath)
                        throw new ApplicationException($"Unable to find I2C Controller containing {Options.DeviceId}");

                    Log.WriteLineVerbose($"Opening {devicepath.Id}...");

                    aqs = I2cDevice.GetDeviceSelector(devicepath.Id);
                }
#endif
                Log.WriteLineVerbose("Opening {0}...",aqs);
                var deviceInfos = await AsAsync(DeviceInformation.FindAllAsync(aqs));

                if (deviceInfos?.FirstOrDefault() == null)
                    throw new ApplicationException("I2C controller not found");

                var id = deviceInfos.First().Id;
                Int32 slaveAddress = Convert.ToInt32(Options.DeviceAddress, 16);

                using (var device = await AsAsync(I2cDevice.FromIdAsync(id, new I2cConnectionSettings(slaveAddress))) )
                {
                    if (null == device)
                        throw new ApplicationException($"Slave address 0x{slaveAddress:X} on bus {id} is in use. Please ensure that no other applications are using this device.");

                    //
                    // Configure Device
                    //

                    // ...

                    //
                    // Dump device info
                    //

                    if (Options.ShowConfig)
                    {
                        // Bus parameters

                        Log.WriteLineRaw($"    Device Id: {device.DeviceId}");
                        Log.WriteLineRaw($"Slave Address: {device.ConnectionSettings.SlaveAddress:X}");
                        Log.WriteLineRaw($"    Bus Speed: {device.ConnectionSettings.BusSpeed}");
                        Log.WriteLineRaw($" Sharing Mode: {device.ConnectionSettings.SharingMode}");

                        // Device serial number

                        var serialA = I2CReadWrite(device, new byte[] { 0xfa, 0x0f }, 8 );
                        var serialB = I2CReadWrite(device, new byte[] { 0xfc, 0xc9 }, 6 );
                        var firmwarerev = I2CReadWrite(device, new byte[] { 0x84, 0xb8 }, 1 );

                        var serialnumberbytes = new byte[] { serialA[0], serialA[2], serialA[4], serialA[6], serialB[0], serialB[1], serialB[3], serialB[4] };
                        var serialnumbersb = serialnumberbytes.Aggregate(new StringBuilder(),(sb,b)=>sb.Append(b.ToString("X")));
                        var serialnumber = serialnumbersb.ToString();

                        var model = $"Si70{serialB[0]}";
                        var firmware = "Unknown";
                        if (firmwarerev[0] == 0xff)
                            firmware = "1.0";
                        else if (firmwarerev[0] == 0x20)
                            firmware = "2.0";

                        Log.WriteLineRaw($"        Model: {model}");
                        Log.WriteLineRaw($"Serial Number: {serialnumber}");
                        Log.WriteLineRaw($" Firmware Rev: {firmware}");
                    }

                    // For reference: https://github.com/robert-hh/SI7021/blob/master/SI7021.py 

                    //
                    // Get some readings
                    //

                    int times = 5;
                    while(times-- > 0)
                    {
                        var humidityreading = I2CReadWrite(device, new byte[] { 0xe5 }, 3 );

                        UInt16 msb = (UInt16)(humidityreading[0]);
                        UInt16 lsb = (UInt16)(humidityreading[1]);
                        UInt16 humidity16 = (UInt16)((msb << 8) | lsb);

                        double humidity = ((double)humidity16 * 125.0) / 65536.0 - 6.0;

                        //             "        Model: 
                        Log.WriteLine($"     Humidity: {humidity:0.0}%");

                        var tempreading = I2CReadWrite(device, new byte[] { 0xe0 }, 2 );

                        msb = (UInt16)(tempreading[0]);
                        lsb = (UInt16)(tempreading[1]);
                        UInt16 temp16 = (UInt16)((msb << 8) | lsb);

                        double temp = ((double)temp16 * 175.72 / 65536.0) - 46.85;

                        //             "        Model: 
                        Log.WriteLine($"  Temperature: {temp:0.0}C");

                        await Task.Delay(500);
                    }

                    //
                    // Init module client
                    //

                    if (Options.UseEdge)
                    {
                        Init().Wait();
                    }

#if nope
                    // Wait until the app unloads or is cancelled
                    //if (Options.Receive || Options.Transmit)
                    {
                        var cts = new CancellationTokenSource();
                        AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                        Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
                        WhenCancelled(cts.Token).Wait();
                    }
#endif
                }
            }
            catch (Exception ex)
            {
                Log.WriteLineException(ex);
            }        
        }

        private static byte[] I2CReadWrite(I2cDevice device, byte[] writeBuffer, int readsize)
        {
            byte[] readBuffer = new byte[readsize];
            var result = device.WriteReadPartial(writeBuffer,readBuffer);
            Log.WriteLineVerbose(result.ToString());

            var display = readBuffer.Aggregate(new StringBuilder(),(sb,b)=>sb.Append(string.Format("{0:X2} ",b)));
            Log.WriteLineVerbose(display.ToString());

            return readBuffer;
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            AmqpTransportSettings amqpSetting = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            ITransportSettings[] settings = { amqpSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Log.WriteLine($"IoT Hub module client initialized.");
        }
    }
}
