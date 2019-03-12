//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using EdgeModuleSamples.Common.Messages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;

namespace UARTLCD
{
    // rolling our own simple color type to avoid referencing any UI dependencies
    public struct RGBColor
    {
        public readonly byte Red;
        public readonly byte Green;
        public readonly byte Blue;
        public RGBColor(byte r, byte g, byte b)
        {
            Red = r;
            Green = g;
            Blue = b;
        }
    }
    public static class Colors
    {
        public readonly static RGBColor Red = new RGBColor(0xff, 0, 0);
        public readonly static RGBColor Green = new RGBColor(0, 0xff, 0);
        public readonly static RGBColor Blue = new RGBColor(0, 0, 0xff);
        public readonly static RGBColor Yellow = new RGBColor(0xff, 0xff, 0);
        public readonly static RGBColor Black = new RGBColor(0, 0, 0);
        public readonly static RGBColor White = new RGBColor(0xff, 0xff, 0xff);
    }

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
            Dictionary<string, RGBColor> FruitColors = new Dictionary<string, RGBColor>()
            {
                {"apple", Colors.Red},
                {"pear", Colors.Yellow},
                {"pen", Colors.Green},
                {"grapes", Colors.Blue}
            };
            AzureConnection connection = null;
            UARTDevice uart = null;
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
                        Log.WriteLine("Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(async () =>
                    {
                        try
                        {
                            Log.WriteLine("creating UART device {0}", Options.DeviceName != null ? Options.DeviceName : "(default)");
                            uart = await UARTDevice.CreateUARTDeviceAsync(Options.DeviceName);
                            await uart.InitAsync();
                            Log.WriteLine("uart initialzed");
                            if (Options.Test)
                            {
                                Log.WriteLine("initiating test");
                                if (Options.TestMessage.Length > 1)
                                {
                                    await uart.TestAsync(Options.TestMessage);
                                } else
                                {
                                    await uart.TestAsync("Test");
                                }
                                Environment.Exit(2);
                            }                            
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("UART exception {0}", e.ToString());
                        }
                    }
                )
            );

            AzureModule m = (AzureModule)connection.Module;
            m.ConfigurationChanged += async (object sender, ConfigurationType newConfiguration) =>
            {
                var module = (AzureModule)sender;
                Log.WriteLine("updating UART_lcd config with {0}", newConfiguration.ToString());
                //await gpio.UpdatePinConfigurationAsync(newConfiguration.GpioPins);
                await Task.CompletedTask;
            };
            m.FruitChanged += async (object sender, string fruit) =>
            {
                Log.WriteLine("fruit changed to {0}", fruit.ToLower());
                var module = (AzureModule)sender;
                await Task.Run(async () => {
                    await uart.SetBackgroundAsync(FruitColors[fruit.ToLower()]);
                    await uart.Clear();
                    await uart.WriteStringAsync(fruit.ToLower());
                });
            };
            await uart.SetBackgroundAsync(Colors.White);
            await uart.WriteStringAsync("Loaded");

            await connection.NotifyModuleLoadAsync();

            Log.WriteLine("Initialization Complete. have connection and device.  ");

            Task.WaitAll(Task.Run(() =>
            {
                try
                {
                    for (; ; )
                    {
                        Log.WriteLine("{0} wait spin", Environment.TickCount);
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("wait spin exception {0}", e.ToString());
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
