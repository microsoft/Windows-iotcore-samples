//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ConsoleDotNetCoreGPIO
{
    class AsyncHelper
    {
        // Work around this problem:
        // https://github.com/Microsoft/dotnet/issues/590
        // https://github.com/dotnet/corefx/issues/22789
        public static async Task<T> SyncFromAsync<T>(IAsyncOperation<T> op, string dbgtag)
        {

            T result = default(T);
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
                result = op.GetResults();
            }

            return result;
        }
        public static async Task SyncFromAsync(IAsyncAction op, string dbgtag)
        {
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
            }

            return;
        }
    }
    class Program
    {
        static async Task<int> MainAsync(string[] args)
        {
            Log.WriteLine("Starting async...");
            var Options = new AppOptions();

            Options.Parse(args);
            Log.Enabled = Options.Logging;
            Log.WriteLine("arg parse complete...");
            AzureConnection connection = null;
            GPIODevice gpio = null;
            await Task.WhenAll(
                Task.Run(async () => {
                    if (!Options.Test.HasValue)
                    {
                        Log.WriteLine("starting connection creation");
                        connection = await AzureConnection.CreateAzureConnectionAsync();
                    }
                }),
                Task.Run(() =>
                    {
                        gpio = new GPIODevice();
                        gpio.InitOutputPins(Options);
                        if (Options.Test.HasValue)
                        {
                            Log.WriteLine("initiating pin test");
                            gpio.Test(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
                        }
                    }
                )
            );
            connection.Module.ConfigurationChanged += async (object sender, ConfigurationType newConfiguration) =>
            {
                var module = (AzureModule)sender;
                Log.WriteLine("updating gpio pin config with {0}", newConfiguration.ToString());
                await gpio.UpdatePinConfigurationAsync(newConfiguration.GpioPins);
            };
            Dictionary<string, string> FruitColors = new Dictionary<string, string>()
            {
                {"apple", "red" },
                {"pear", "yellow" },
                {"pen", "green" },
                {"grapes", "blue"}
            };
            connection.Module.FruitChanged += async (object sender, string fruit) =>
            {
                Log.WriteLine("fruit changed to {0}", fruit.ToLower());
                var module = (AzureModule)sender;
                await Task.Run(() => gpio.ActivePin = FruitColors[fruit.ToLower()]);
            };
            await Task.Run(async () =>
            {
                Log.WriteLine("initializing gpio pin config with {0}", connection.Module.Configuration.GpioPins);
                await gpio.UpdatePinConfigurationAsync(connection.Module.Configuration.GpioPins);
            });


            Log.WriteLine("Initialization Complete. have connection and device pins.  Active Pin is {0}", gpio.ActivePin == null ? "(null)" : gpio.ActivePin);

            Task.WaitAll(Task.Run(() =>
            {
                for (; ; )
                {
                    Log.WriteLine("{0} wait spin", Environment.TickCount);
                    gpio.LogInputPins();
                    Thread.Sleep(TimeSpan.FromSeconds(30));
                }
            }));
#if DISABLE            
            var w = new EventWaitHandle(false, EventResetMode.ManualReset);
            for (; ; )
            {
                Log.WriteLine("{0} waiting spin", Environment.TickCount);
                w.WaitOne(TimeSpan.FromSeconds(30));
            }
#endif
            return 0;
        }

        static int Main(string[] args)
        {
            Log.Enabled = true;
            Log.WriteLine("Starting...");
            int rc = 0;
            try
            {
                Task.WaitAll(Task.Run(async () =>
                    rc = await MainAsync(args))
                );
            }
            catch (Exception e)
            {
                Log.WriteLineError("app failed {0}", e.ToString());
                rc = 1;
            }
            Log.WriteLine("Complete....");
            Console.Out.Flush();
            return rc;

        }
    }
}
