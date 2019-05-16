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
    using Windows.Devices.Enumeration;
    using Windows.Devices.Gpio;
    using Windows.Devices.I2c;

    using EdgeModuleSamples.Common.Logging;
    using EdgeModuleSamples.Devices;
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
                // TODO: Options.List
                if (Options.Exit)
                    return;

                //
                // Open Device
                //

                using (var device = await Si7021.Open() )
                {
                    if (null == device)
                        throw new ApplicationException($"Unable to open sensor. Please ensure that no other applications are using this device.");

                    //
                    // Dump device info
                    //

                    Log.WriteLine($"Model: {device.Model}");
                    Log.WriteLine($"Serial Number: {device.SerialNumber}");
                    Log.WriteLine($"Firmware Rev: {device.FirmwareRevision}");

                    //
                    // Init module client
                    //

                    if (Options.UseEdge)
                    {
                        Init().Wait();
                    }

                    //
                    // Launch background thread to obtain readings
                    //


                    var background = Task.Run(async ()=>
                    {
                        while(true)
                        {
                            device.Update();

                            var message = new MessageBody();
                            message.Ambient.Temperature = device.Temperature;
                            message.Ambient.Humidity = device.Humidity;
                            message.TimeCreated = DateTime.Now;

                            string dataBuffer = JsonConvert.SerializeObject(message); 
                            var eventMessage = new Message(Encoding.UTF8.GetBytes(dataBuffer));
                            Log.WriteLine($"SendEvent: [{dataBuffer}]");

                            if (Options.UseEdge)
                            {
                                await ioTHubModuleClient.SendEventAsync("temperatureOutput", eventMessage);                        
                            }

                            await Task.Delay(1000);
                        }
                    });

                    //
                    // Wait until the app unloads or is cancelled
                    //

                    var cts = new CancellationTokenSource();
                    AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                    Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
                    WhenCancelled(cts.Token).Wait();
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
