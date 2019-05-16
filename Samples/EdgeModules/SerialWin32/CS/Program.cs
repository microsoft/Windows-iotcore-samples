//
// Copyright (c) Microsoft. All rights reserved.
//
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

    using EdgeModuleSamples.Common.Logging;

    class Program
    {
        static AppOptions Options;
        static ModuleClient ioTHubModuleClient;
        static readonly Random Rnd = new Random();

        static void Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);
                Log.Enabled = !Options.Quiet;
                Log.Verbose = Options.Verbose;

                //
                // Enumerate devices
                //

                var devicepaths = Win32Serial.Device.EnumerateDevices();
                if (Options.List || string.IsNullOrEmpty(Options.DeviceName))
                {
                    if (devicepaths.Length > 0)
                    {
                        Log.WriteLine("Available devices:");

                        foreach (var devicepath in devicepaths)
                        {
                            Log.WriteLine($"{devicepath}");
                        }
                    }
                    else
                    {
                        Log.WriteLine("No available devices");
                    }
                    return;
                }

                //
                // Open Device
                //

                var deviceid = devicepaths.Where(x => x.Contains(Options.DeviceName)).SingleOrDefault();
                if (null == deviceid)
                    throw new ApplicationException($"Unable to find device containing {Options.DeviceName}");

                Log.WriteLine($"{DateTime.Now.ToLocalTime()} Connecting to device {deviceid}...");

                using (var device = Win32Serial.Device.Create(deviceid))
                {
                    //
                    // Configure Device
                    //

                    var config = device.Config;
                    config.BaudRate = 115200;
                    device.Config = config;

                    var timeouts = device.Timeouts;
                    timeouts.ReadIntervalTimeout = 10;
                    timeouts.ReadTotalTimeoutConstant = 0;
                    timeouts.ReadTotalTimeoutMultiplier = 0;
                    timeouts.WriteTotalTimeoutConstant = 0;
                    timeouts.WriteTotalTimeoutConstant = 0;
                    device.Timeouts = timeouts;

                    //
                    // Dump device info
                    //

                    if (Options.ShowConfig)
                    {
                        Log.WriteLine("=====================================");

                        foreach (var line in device.Info)
                        {
                            Log.WriteLine(line);
                        }

                        Log.WriteLine("=====================================");
                    }

                    //
                    // Init module client
                    //

                    if (Options.UseEdge)
                    {
                        Init().Wait();
                    }

                    //
                    // Set up a background thread to read from the device
                    //

                    if (Options.Receive)
                    {
                        var background = Task.Run(() => ReaderTask(device));
                    }

                    //
                    // Continuously write to serial device every second
                    //

                    if (Options.Transmit)
                    {
                        var background = Task.Run(() => TransmitTask(device));
                    }

                    // Wait until the app unloads or is cancelled
                    if (Options.Receive || Options.Transmit)
                    {
                        var cts = new CancellationTokenSource();
                        AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                        Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
                        WhenCancelled(cts.Token).Wait();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLineException(ex);
            }        
        }

        private static void TransmitTask(Win32Serial.Device device)
        {
            // Create write overlapped structure for async operation
            var overlapped = Win32Serial.Device.CreateOverlapped();

            uint numbytes;
            int i = 1;
            const uint size = MessageBody.SerialSize;
            int limit = 15;
            if (Options.TestCount.HasValue)
            {
                limit = Options.TestCount.Value;
            }
            while (!Options.Test || i <= limit)
            {
                // Come up with a new message

                var tempData = new MessageBody
                {
                    Machine = new Machine
                    {
                        Temperature = Rnd.NextDouble() * 100.0,
                        Pressure = Rnd.NextDouble() * 100.0,
                    },
                    Ambient = new Ambient
                    {
                        Temperature = Rnd.NextDouble() * 100.0,
                        Humidity = Rnd.Next(24, 27)
                    },
                    Number = i
                };

                // Async write, using overlapped structure
                var message = tempData.SerialEncode;
                device.Write(Encoding.ASCII.GetBytes(message), size, out numbytes, ref overlapped);
                Log.WriteLineVerbose($"Write {i} Started");

                // Block until write completes
                device.GetOverlappedResult(ref overlapped, out numbytes, true);

                Log.WriteLine($"Write {i} Completed. Wrote {numbytes} bytes: \"{message}\"");
                i++;
                Thread.Sleep(1000);
            }            
            Environment.Exit(0);
        }

        private static async void ReaderTask(Win32Serial.Device device)
        {
            try
            {
                //
                // Continuously read from serial device
                // and send those values to edge hub.
                //

                // Create write overlapped structure for async operation
                var overlapped = Win32Serial.Device.CreateOverlapped();

                int i = 0;
                uint numbytes;
                const uint size = MessageBody.SerialSize;
                var inbuf = new byte[size];
                while (true)
                {
                    // Clear input buffer
                    Array.Clear(inbuf, 0, inbuf.Length);

                    // Start Async Read, using overlapped structure
                    device.Read(inbuf, size, out numbytes, ref overlapped);
                    Log.WriteLineVerbose($"Async Read {i} Started");

                    // Block until Read finishes
                    device.GetOverlappedResult(ref overlapped, out numbytes, true);
                    var message = Encoding.ASCII.GetString(inbuf);
                    Log.WriteLine($"Async Read {i} Completed. Received {numbytes} bytes: \"{message}\"");

                    // Send it over Edge as a messagebody
                    if (Options.UseEdge)
                    {
                        var tempData = new MessageBody();
                        tempData.SerialEncode = message;
                        tempData.TimeCreated = DateTime.Now;
                        if (tempData.isValid)
                        {
                            string dataBuffer = JsonConvert.SerializeObject(tempData); 
                            var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                            Log.WriteLine($"SendEvent: [{dataBuffer}]");

                            await ioTHubModuleClient.SendEventAsync("temperatureOutput", eventMessage);                        
                        }
                        else
                        {
                            Log.WriteLineError($"Invalid temp data");
                        }
                    }

                    i++;
                }
            }
            catch (Exception ex)
            {
                Log.WriteLineException(ex);
            }
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
