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

namespace PWMFruit
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
            if (Options.List)
            {
                await PWMDevice.ListPWMDevicesAsync();
            }
            // speed as int pct 0-100
            Dictionary<string, int> FruitSpeed = new Dictionary<string, int>()
            {
                {"apple", 50 },
                {"pear", 50 },
                {"pen", 0 },
                {"grapes", 100},
                {"other", 0}
            };
            AzureConnection connection = null;
            PWMDevice pwm = null;
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
                        Log.WriteLine("PWM Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(async () =>
                    {
                        try
                        {
                            // TODO: fix appoptions to allow pin number to be specified.
                            pwm = await PWMDevice.CreatePWMDeviceAsync(Options.DeviceName, 0);

                       
                            if (Options.Test)
                            {
                                Log.WriteLine("initiating pin test");
                                var ts = TimeSpan.FromSeconds(15);
                                var tc = 80;
                                if (Options.TestTime.HasValue)
                                {
                                    ts = Options.TestTime.Value;
                                }
                                if (Options.TestCount.HasValue)
                                {
                                    tc = Options.TestCount.Value;
                                }
                                pwm.Test(ts, tc);
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("PWM InitOutputPins exception {0}", e.ToString());
                        }
                    }
                )
            );
            AzureModule m = null;
            EventHandler <ConfigurationType> ConfigurationChangedHandler = async (object sender, ConfigurationType newConfiguration) =>
            {
                var module = (AzureModule)sender;
                Log.WriteLine("updating pwm pin config with {0}", newConfiguration.ToString());
                //await pwm.UpdatePinConfigurationAsync(newConfiguration.GpioPins);
                await Task.CompletedTask;
            };
            EventHandler<string> FruitChangedHandler = (object sender, string fruit) =>
            {
                Log.WriteLine("fruit changed to {0}", fruit.ToLower());
                var module = (AzureModule)sender;
                pwm.SetSpeed(FruitSpeed[fruit.ToLower()]);
            };
            if (!Options.Test)
            {
                m = (AzureModule)connection.Module;
                m.ConfigurationChanged += ConfigurationChangedHandler;
                m.FruitChanged += FruitChangedHandler;
                await Task.Run(async () =>
                {
                    try
                    {
                        //Log.WriteLine("initializing pwm pin config with {0}", m.Configuration.GpioPins);
                        //await pwm.UpdatePinConfigurationAsync(m.Configuration.GpioPins);
                        await Task.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("GPIO UpdatePinConfig Lambda exception {0}", e.ToString());
                    }
                });
                await connection.NotifyModuleLoadAsync();
            }
            try
            {
                Log.WriteLine("Initialization Complete. have {0}", Options.Test ? "device" : "connection and device");

                Task.WaitAll(Task.Run(() =>
                {
                    try
                    {
                        for (; ; )
                        {
                            Log.WriteLine("{0} wait spin", Environment.TickCount);
                            //pwm.LogInputPins();
                            Thread.Sleep(TimeSpan.FromSeconds(30));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("PWM wait spin exception {0}", e.ToString());
                    }

                }));
            } finally
            {
                if (!Options.Test)
                {
                    m.ConfigurationChanged -= ConfigurationChangedHandler;
                    m.FruitChanged -= FruitChangedHandler;
                }
                if (connection != null)
                {
                    connection.Dispose();
                }
                pwm.Dispose();
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
