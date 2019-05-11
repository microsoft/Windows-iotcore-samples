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

namespace I2CMPU6050
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
            AzureConnection connection = null;
            I2CMpuDevice mpu = null;
            // TODO: Options.List
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
                        Log.WriteLine("I2c Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(async () =>
                    {
                        try
                        {
                            Log.WriteLine("creating mpu device {0}", Options.DeviceName != null ? Options.DeviceName : "(default)");
                            mpu = await I2CMpuDevice.CreateMpuDeviceAsync(Options.DeviceName);
                            mpu.InitAsync().Wait();
                            if (Options.Test)
                            {
                                Log.WriteLine("initiating test");
                                if (Options.Test)
                                {
                                    mpu.Test(Options.TestTime.Value);
                                } else
                                {
                                    mpu.Test(TimeSpan.FromSeconds(15));
                                }
                                Environment.Exit(2);
                            }                            
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("I2c exception {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }
                )
            );
            try
            {
                EventHandler<string> ModuleLoadedHandler = async (Object sender, string moduleName) =>
                {
                    try
                    {
                        Log.WriteLine("module loaded.   resending state");
                        await connection.NotifyNewModuleOfCurrentStateAsync();
                    }
                    catch (Exception e)
                    {
                        Log.WriteLineError("failed to notify state {0}", e.ToString());
                        Environment.Exit(2);
                    }
                };
                if (connection != null)
                {
                    ((AzureModule)(connection.Module)).ModuleLoaded += ModuleLoadedHandler;
                }

                EdgeModuleSamples.Common.Device.MpuDevice.OrientationEventHandler OrientationChangedHandler = (device, change) =>
                {
                    connection.UpdateObject(new KeyValuePair<string, object>(Keys.Orientation, change.newOrientation));
                };
                mpu.OrientationChanged += OrientationChangedHandler;
                try
                {
                    Log.WriteLine("Initialization Complete. have connection and device.  ");

                    Task.WaitAll(Task.Run(async () =>
                    {
                        try
                        {
                            await mpu.BeginOrientationMonitoringAsync();
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("I2c wait spin exception {0}", e.ToString());
                        }

                    }));
                } finally
                {
                    mpu.OrientationChanged -= OrientationChangedHandler;
                    if (connection != null)
                    {
                        ((AzureModule)(connection.Module)).ModuleLoaded -= ModuleLoadedHandler;
                    }
                }
            } finally
            {
                if (connection != null)
                {
                    connection.Dispose();
                }
                mpu.Dispose();
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
