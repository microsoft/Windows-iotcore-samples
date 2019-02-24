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

                        var serial1 = I2CReadWrite(device, new byte[] { 0xfa, 0x0f }, 8 );
                        var serial2 = I2CReadWrite(device, new byte[] { 0xfc, 0xc9 }, 6 );
                        var firmwarerev = I2CReadWrite(device, new byte[] { 0x84, 0xb8 }, 1 );
                    }

                    //
                    // Get some readings
                    //

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

            var display = readBuffer.Aggregate(new StringBuilder(),(sb,b)=>sb.Append(string.Format("{0:X} ",b)));
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
