//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace ConsoleDotNetCoreGPIO
{
    class Program
    {
        static async Task<int> MainAsync(string[] args)
        {
            Log.WriteLine("Starting async...");
            var Options = new AppOptions();

            Options.Parse(args);
            Log.Enabled = !Options.Quiet;
            Log.Verbose = Options.Verbose;
            Log.WriteLine("arg parse complete...");
            Dictionary<string, string> FruitColors = new Dictionary<string, string>()
            {
                {"apple", "red" },
                {"pear", "yellow" },
                {"pen", "green" },
                {"grapes", "blue"}
            };
            AzureConnection connection = null;
            GPIODevice gpio = null;
            await Task.WhenAll(
                Task.Run(async () => {
                    try { 
                        if (!Options.Test.HasValue)
                        {
                            Log.WriteLine("starting connection creation");
                            connection = await AzureConnection.CreateAzureConnectionAsync();
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GPIO Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(() =>
                    {
                        try
                        {
                            gpio = new GPIODevice();
                            gpio.InitOutputPins(Options);
                            if (Options.Test.HasValue)
                            {
                                Log.WriteLine("initiating pin test");
                                gpio.Test(Options.Test.Value, TimeSpan.FromSeconds(2));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("GPIO InitOutputPins exception {0}", e.ToString());
                        }
                    }
                )
            );
            AzureModule m = (AzureModule)connection.Module;
            m.ConfigurationChanged += async (object sender, ConfigurationType newConfiguration) =>
            {
                var module = (AzureModule)sender;
                Log.WriteLine("updating gpio pin config with {0}", newConfiguration.ToString());
                await gpio.UpdatePinConfigurationAsync(newConfiguration.GpioPins);
            };
            m.FruitChanged += async (object sender, string fruit) =>
            {
                Log.WriteLine("fruit changed to {0}", fruit.ToLower());
                var module = (AzureModule)sender;
                await Task.Run(() => gpio.ActivePin = FruitColors[fruit.ToLower()]);
            };
            m.OrientationChanged += async (object sender, EdgeModuleSamples.Common.Orientation o) =>
            {
                Log.WriteLine("orientation changed to {0}", o.ToString());
                var module = (AzureModule)sender;
                await Task.Run(() => gpio.InvertOutputPins());
            };
            await Task.Run(async () =>
            {
                try { 
                    Log.WriteLine("initializing gpio pin config with {0}", m.Configuration.GpioPins);
                    await gpio.UpdatePinConfigurationAsync(m.Configuration.GpioPins);
                }
                catch (Exception e)
                {
                    Log.WriteLine("GPIO UpdatePinConfig Lambda exception {0}", e.ToString());
                }
            });
            await connection.NotifyModuleLoadAsync();

            Log.WriteLine("Initialization Complete. have connection and device pins.  Active Pin is {0}", gpio.ActivePin == null ? "(null)" : gpio.ActivePin);

            Task.WaitAll(Task.Run(() =>
            {
                try { 
                    for (; ; )
                    {
                        Log.WriteLine("{0} wait spin", Environment.TickCount);
                        gpio.LogInputPins();
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("GPIO wait spin exception {0}", e.ToString());
                }

            }));
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
