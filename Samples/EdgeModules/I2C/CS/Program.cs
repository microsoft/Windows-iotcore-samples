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

namespace ConsoleDotNetCoreI2c
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
            I2cDevice I2c = null;
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
                        Log.WriteLine("I2c Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(async () =>
                    {
                        try
                        {
                            I2c = await I2cDevice.CreateI2cDevice();
                            if (Options.Test.HasValue)
                            {
                                Log.WriteLine("initiating test");
                                I2c.Test(TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(2));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("I2c exception {0}", e.ToString());
                        }
                    }
                )
            );
            AzureModule m = (AzureModule)connection.Module;
            await connection.NotifyModuleLoad();

            Log.WriteLine("Initialization Complete. have connection and device.  ");

            Task.WaitAll(Task.Run(() =>
            {
                try { 
                    for (; ; )
                    {
                        Log.WriteLine("{0} wait spin", Environment.TickCount);
                        Thread.Sleep(TimeSpan.FromSeconds(30));
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLine("I2c wait spin exception {0}", e.ToString());
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
