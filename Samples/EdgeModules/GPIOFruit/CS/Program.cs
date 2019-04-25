//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using EdgeModuleSamples.Common.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace GPIOFruit
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
            // TODO: Options.List
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
                        if (!Options.Test)
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
                            if (Options.Test)
                            {
                                Log.WriteLine("initiating pin test");
                                if (Options.TestTime.HasValue)
                                {
                                    gpio.Test(Options.TestTime.Value, TimeSpan.FromSeconds(2));
                                } else
                                {
                                    gpio.Test(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("GPIO InitOutputPins exception {0}", e.ToString());
                        }
                    }
                )
            );
            try
            {
                AzureModule m = (AzureModule)connection.Module;
                Orientation currentOrientation = Orientation.RightSideUp;
                EventHandler<ConfigurationType> ConfigurationChangedHandler = async (object sender, ConfigurationType newConfiguration) =>
                {
                    var module = (AzureModule)sender;
                    Log.WriteLine("updating gpio pin config with {0}", newConfiguration.ToString());
                    await gpio.UpdatePinConfigurationAsync(newConfiguration.GpioPins);
                };
                m.ConfigurationChanged += ConfigurationChangedHandler;
                try
                {
                    EventHandler<string> FruitChangedHandler = async (object sender, string fruit) =>
                    {
                        Log.WriteLine("fruit changed to {0}", fruit.ToLower());
                        var module = (AzureModule)sender;
                        string color = null;
                        if (FruitColors.ContainsKey(fruit.ToLower()))
                        {
                            color = FruitColors[fruit.ToLower()];
                        }
                        await Task.Run(() => gpio.ActivePin = color);
                    };
                    m.FruitChanged += FruitChangedHandler;
                    try
                    {
                        EventHandler<Orientation> OrientationChangedHandler = async (object sender, EdgeModuleSamples.Common.Orientation o) =>
                        {
                            Log.WriteLine("OrientationChanged sent {0}", o.ToString());
                            //var module = (AzureModule)sender;
                            Log.WriteLine("Current Orientation {0}", currentOrientation.ToString());
                            if (o != currentOrientation)
                            {
                                currentOrientation = o;
                                Log.WriteLine("Orientation changing to {0}", o.ToString());
                                await Task.Run(() => gpio.InvertOutputPins());
                            }
                            else
                            {
                                Log.WriteLine("Orientation already correct -- skipping", o.ToString());
                                await Task.CompletedTask;
                            }
                        };
                        m.OrientationChanged += OrientationChangedHandler;
                        try
                        {
                            await Task.Run(async () =>
                            {
                                try
                                {
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
                                try
                                {
                                    // TODO: cancellation token
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
                        }
                        finally
                        {
                            m.OrientationChanged -= OrientationChangedHandler;
                        }
                    }
                    finally
                    {
                        m.FruitChanged -= FruitChangedHandler;
                    }
                }
                finally
                {
                    m.ConfigurationChanged += ConfigurationChangedHandler;
                }
            }
            finally
            {
                gpio.Dispose();
                if (connection != null)
                {
                    connection.Dispose();
                }
            }

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
