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
using Windows.Media.Audio;
using Windows.Media.MediaProperties;

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
            var inDevice = default(AudioInputDevice);
            var outDevice = default(AudioOutputDevice);
            var connection = default(AzureConnection);
            var module = default(AzureModule);
            try
            {

                if (Options.List)
                {
                    await AudioInputDevice.ListDevicesAsync();
                }
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (!Options.Test)
                            {
                                Log.WriteLine("starting connection creation");
                                connection = await AzureConnection.CreateAzureConnectionAsync();
                            }
                            else
                            {
                                Log.WriteLine("test mode. skipping connection creation");
                            }
                        }
                        catch (Exception e)
                        {
                            Log.WriteLine("Audio Main CreateAzureConnectionAsync exception {0}", e.ToString());
                        }
                    }),
                    Task.Run(async () =>
                        {
                            try
                            {
                                var settings = new AudioGraphSettings(Windows.Media.Render.AudioRenderCategory.Speech);
                                settings.PrimaryRenderDevice = await AudioOutputDevice.SelectAsync(Options.OutputDeviceName);
                                Log.WriteLine($"found Primary Render Device {settings.PrimaryRenderDevice.Id}");
                                var graph = await AsyncHelper.AsAsync(AudioGraph.CreateAsync(settings));
                                if (graph.Status != AudioGraphCreationStatus.Success)
                                {
                                    throw new ApplicationException($"Audio Graph Creation failed status = {graph.Status} err = {graph.ExtendedError}");
                                }
                                Log.WriteLine("Graph created");
                                inDevice = new AudioInputDevice(graph.Graph);
                                Log.WriteLine("input device created");
                                var encSettings = AudioEncodingProperties.CreatePcm(16000, 1, 16);
                                await inDevice.InitializeAsync(await AudioInputDevice.SelectAsync(Options.InputDeviceName), encSettings);
                                Log.WriteLine("input device Initialized");
                                outDevice = new AudioOutputDevice(graph.Graph);
                                Log.WriteLine("output device created");
                                await outDevice.InitializeAsync();
                                Log.WriteLine("output device Initialized");
                                inDevice.Connect(outDevice);
                                graph.Graph.Start();
                                Log.WriteLine("graph started");
                            }
                            catch (Exception e)
                            {
                                Log.WriteLine("Audio Initialization exception {0}", e.ToString());
                                Environment.Exit(2);
                            }
                        }
                    )
                );
                EventHandler<ConfigurationType> ConfigurationChangedHandler = async (object sender, ConfigurationType newConfiguration) =>
                {
                    var m = (AzureModule)sender;
                    Log.WriteLine("updating Audio with {0}", newConfiguration.ToString());
                    await Task.CompletedTask;
                };
                try
                {

                    if (!Options.Test)
                    {
                        module = (AzureModule)connection.Module;
                        module.ConfigurationChanged += ConfigurationChangedHandler;
                        await connection.NotifyModuleLoadAsync();
                    }

                    Log.WriteLine("Initialization Complete. have connection and devices");

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
                    if (!Options.Test)
                    {
                        module.ConfigurationChanged += ConfigurationChangedHandler;
                    }
                }
            }
            finally
            {
                if (connection != default(AzureConnection))
                {
                    connection.Dispose();
                }
                if (module != default(AzureModule))
                {
                    module.Dispose();
                }
                if (inDevice != default(AudioInputDevice))
                {
                    inDevice.Dispose();
                }
                if (outDevice != default(AudioOutputDevice))
                {
                    outDevice.Dispose();
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
