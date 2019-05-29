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

namespace AudioLoopback
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
            AudioDevice audio = null;
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
                        Log.WriteLine("Audio Main CreateAzureConnectionAsync exception {0}", e.ToString());
                    }
                }),
                Task.Run(() =>
                    {
                        try
                        {
                            audio = new AudioDevice();
                            if (Options.Test)
                            {
                                Log.WriteLine("initiating pin test");
                                if (Options.TestTime.HasValue)
                                {
                                    audio.Test(Options.TestTime.Value, TimeSpan.FromSeconds(2));
                                } else
                                {
                                    audio.Test(TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(2));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("Audio InitOutputPins exception {0}", e.ToString());
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
                    Log.WriteLine("updating Audio with {0}", newConfiguration.ToString());
                };
                m.ConfigurationChanged += ConfigurationChangedHandler;
                try
                {
                    await connection.NotifyModuleLoadAsync();

                    Log.WriteLine("Initialization Complete. have connection and device pins.  Active Pin is {0}", Audio.ActivePin == null ? "(null)" : Audio.ActivePin);

                    Task.WaitAll(Task.Run(() =>
                    {
                        try
                        {
                            // TODO: cancellation token
                            for (; ; )
                            {
                                Log.WriteLine("{0} wait spin", Environment.TickCount);
                                Thread.Sleep(TimeSpan.FromSeconds(30));
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("Audio wait spin exception {0}", e.ToString());
                        }

                    }));
                }
                finally
                {
                    m.ConfigurationChanged += ConfigurationChangedHandler;
                }
            }
            finally
            {
                audio.Dispose();
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
