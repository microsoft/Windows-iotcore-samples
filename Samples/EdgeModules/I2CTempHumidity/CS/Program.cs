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

    using EdgeModuleSamples.Common;

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

                //
                // Enumerate devices
                //

                // ...

                //
                // Open Device
                //

                // ...


                //using (...)
                {
                    //
                    // Configure Device
                    //

                    // ...

                    //
                    // Dump device info
                    //


                    if (Options.ShowConfig)
                    {
                        // ...
                    }

                    //
                    // Init module client
                    //

                    if (Options.UseEdge)
                    {
                        Init().Wait();
                    }


                    // Wait until the app unloads or is cancelled
                    //if (Options.Receive || Options.Transmit)
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
