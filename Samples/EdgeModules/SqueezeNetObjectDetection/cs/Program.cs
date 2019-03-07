//
// Copyright (c) Microsoft. All rights reserved.
//
using Microsoft.Azure.Devices.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Loader;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Media;

using EdgeModuleSamples.Common.Logging;
using static EdgeModuleSamples.Common.AsyncHelper;
using static Helpers.BlockTimerHelper;

namespace SampleModule
{
    class ImageInference
    {
        // globals
        private static readonly string _labelsFileName = "Labels.json";
        private static AppOptions Options;
        private static ModuleClient ioTHubModuleClient;
        private static CancellationTokenSource cts = null;
        private static List<string> labels;

        static async Task<int> Main(string[] args)
        {
            try
            {
                //
                // Parse options
                //

                Options = new AppOptions();

                Options.Parse(args);
                Log.Enabled = !Options.Quiet;
                Log.Verbose = Options.Verbose;

                if (Options.List)
                {
                    var devices = await FrameSource.GetSourceNamesAsync();

                    Log.WriteLine("Available cameras:");

                    foreach (var device in devices)
                    {
                        Log.WriteLine(device);
                    }
                    Environment.Exit(1);
                }

                if (string.IsNullOrEmpty(Options.DeviceId))
                    throw new ApplicationException("Please use --device to specify which camera to use");

                //
                // Init module client
                //

                if (Options.UseEdge)
                {
                    Log.WriteLine($"{AppOptions.AppName} module starting.");
                    await BlockTimer("Initializing Azure IoT Edge", async () => await InitEdge());
                }

                cts = new CancellationTokenSource();
                AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
                Console.CancelKeyPress += (sender, cpe) => cts.Cancel();

                //
                // Load model
                //

                ScoringModel model = null;
                await BlockTimer($"Loading modelfile '{Options.ModelPath}' on the {(Options.UseGpu ? "GPU" : "CPU")}",
                    async () => {
                        var d = Directory.GetCurrentDirectory();
                        var path = d + "\\" + Options.ModelPath;
                        StorageFile modelFile = await AsAsync(StorageFile.GetFileFromPathAsync(path));
                        model = await ScoringModel.CreateFromStreamAsync(modelFile,Options.UseGpu);
                    });

                //
                // Load labels
                //

                var fileString = File.ReadAllText(_labelsFileName);
                var fileDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(fileString);
                labels = fileDict.Values.ToList();

                //
                // Open camera
                //

                using (var frameSource = new FrameSource())
                {
                    await frameSource.StartAsync(Options.DeviceId,Options.UseGpu);

                    //
                    // Main loop
                    //
                    do
                    {
                        Log.WriteLineVerbose("Getting frame...");
                        using (var frame = await frameSource.GetFrameAsync())
                        {
                            var inputImage = frame.VideoMediaFrame.GetVideoFrame();
                            ImageFeatureValue imageTensor = ImageFeatureValue.CreateFromVideoFrame(inputImage);

                            //
                            // Evaluate model
                            //

                            ScoringOutput outcome = null;
                            var evalticks = await BlockTimer("Running the model",
                                async () =>
                                {
                                    var input = new ScoringInput() { data_0 = imageTensor };
                                    outcome = await model.EvaluateAsync(input);
                                });

                            //
                            // Print results
                            //

                            var message = ResultsToMessage(outcome);
                            message.metrics.evaltimeinms = evalticks;
                            var json = JsonConvert.SerializeObject(message);
                            Log.WriteLine($"Recognized {json}");

                            //
                            // Send results to Edge
                            //

                            if (Options.UseEdge)
                            { 
                                var eventMessage = new Message(Encoding.UTF8.GetBytes(json));
                                await ioTHubModuleClient.SendEventAsync("resultsOutput", eventMessage); 

                                // Let's not totally spam Edge :)
                                await Task.Delay(500);
                            }
                        }
                    }
                    while (Options.RunForever && ! cts.Token.IsCancellationRequested);

                    await frameSource.StopAsync();
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log.WriteLineError($"{ex.GetType().Name} {ex.Message}");
                return -1;
            }
        }
        
        private static MessageBody ResultsToMessage(ScoringOutput outcome)
        {
            var resultVector = outcome.softmaxout_1.GetAsVectorView();
            
            // Find the top 3 probabilities
            List<float> topProbabilities = new List<float>() { 0.0f, 0.0f, 0.0f };
            List<int> topProbabilityLabelIndexes = new List<int>() { 0, 0, 0 };
            // SqueezeNet returns a list of 1000 options, with probabilities for each, loop through all
            for (int i = 0; i < resultVector.Count(); i++)
            {
                // is it one of the top 3?
                for (int j = 0; j < 3; j++)
                {
                    if (resultVector[i] > topProbabilities[j])
                    {
                        topProbabilityLabelIndexes[j] = i;
                        topProbabilities[j] = resultVector[i];
                        break;
                    }
                }
            }

            var message = new MessageBody();
            message.results = new LabelResult[3];

            for (int i = 0; i < 3; i++)
            {
                message.results[i] = new LabelResult() { label = labels[topProbabilityLabelIndexes[i]], confidence = topProbabilities[i] };
            }

            return message;
        }

        
        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        private static async Task InitEdge()
        {
            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(TransportType.Amqp);

            Log.WriteLineVerbose("CreateFromEnvironmentAsync OK");

            await ioTHubModuleClient.OpenAsync();

            Log.WriteLineVerbose("OpenAsync OK");

            Log.WriteLine($"IoT Hub module client initialized.");
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
    }
}