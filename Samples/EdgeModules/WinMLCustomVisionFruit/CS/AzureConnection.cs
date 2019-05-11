//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Azure;
using EdgeModuleSamples.Common.Logging;
using EdgeModuleSamples.Common.Messages;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace WinMLCustomVisionFruit
{
    public class AzureModule : AzureModuleBase {
        public event EventHandler<string> ModuleLoaded;
        public override string ModuleId { get { return Keys.WinMLModuleId; } }

        async Task<MessageResponse> ProcessModuleLoadedMessage(ModuleLoadedMessage msg)
        {
            await Task.Run(() => {
                try
                {
                    ModuleLoaded?.Invoke(this, msg.ModuleName);
                } catch (Exception e)
                {
                    Log.WriteLine("ModuleLoadedMessageHandler event lambda exception {0}", e.ToString());
                }
                });
            return MessageResponse.Completed;
        }
        static async Task<MessageResponse> ModuleLoadedMessageHandler(Message msg, object ctx)
        {
            AzureModule module = (AzureModule)ctx;
            var msgBytes = msg.GetBytes();
            var msgString = Encoding.UTF8.GetString(msgBytes);
            Log.WriteLine("loadModule msg received: '{0}'", msgString);
            var loadMsg = JsonConvert.DeserializeObject<ModuleLoadedMessage>(msgString);
            await module.ProcessModuleLoadedMessage(loadMsg);
            return MessageResponse.Completed;
        }
        private async Task<MethodResponse> SetModuleLoaded(MethodRequest req, object context)
        {
            string data = Encoding.UTF8.GetString(req.Data);
            Log.WriteLine("Direct Method ModuleLoaded {0}", data);
            var loadMsg = JsonConvert.DeserializeObject<ModuleLoadedMessage>(data);
            AzureModule module = (AzureModule)context;
            await module.ProcessModuleLoadedMessage(loadMsg);
            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + req.Name + "\"}";
            return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
        }

        public AzureModule() {
        }
        public override async Task AzureModuleInitAsync<C>(C c)
        {
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            await _moduleClient.SetInputMessageHandlerAsync(Keys.ModuleLoadedInputRoute, ModuleLoadedMessageHandler, this);
            await _moduleClient.SetMethodHandlerAsync(Keys.SetModuleLoaded, SetModuleLoaded, this);
            await base.AzureModuleInitEndAsync();
        }

    };
    public class AzureConnection : AzureConnectionBase
    {
        private byte[] _lastFruitBody;
        public AzureConnection() {
            _lastFruitBody = new byte[0];
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync()
        {
            return await CreateAzureConnectionAsync<AzureConnection, AzureModule>();
        }

        public override async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\t\tWinML UpdateObjectAsync override kvp = {0}", kvp.ToString());
            if (kvp.Key == Keys.FruitSeen)
            {
                Log.WriteLine("\t\t\t\t\t\tWinML UpdateObjectAsync override FruitSeen D2C msg");
                // output the event stream
                var msgvalue = new FruitMessage();
                msgvalue.OriginalEventUTCTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).ToString("o");
                msgvalue.FruitSeen = (string)kvp.Value;
                byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgvalue));
                lock (_lastFruitBody)
                {
                    _lastFruitBody = msgbody;
                }
                await Task.WhenAll(
                    Task.Run(async () =>
                        {
                            try { 
                                Log.WriteLineVerbose("\t\t\t\t\t\tWinML UpdateObjectAsync to OutputFruit0 kvp = {0}", kvp.ToString());
                                await Module.SendMessageAsync(Keys.OutputFruit0, msgbody);
                            }
                            catch (Exception e)
                            {
                                Log.WriteLineError("WinML UpdateObjectAsync failed to send outputfruit0 {0}", e.ToString());
                                Environment.Exit(2);
                            }
                        }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            Log.WriteLineVerbose("\t\t\t\t\t\tWinML UpdateObjectAsync to OutputFruit1 kvp = {0}", kvp.ToString());
                            await Module.SendMessageAsync(Keys.OutputFruit1, msgbody);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML UpdateObjectAsync failed to send outputfruit1 {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            Log.WriteLineVerbose("\t\t\t\t\t\tWinML UpdateObjectAsync to OutputFruit2 kvp = {0}", kvp.ToString());
                            await Module.SendMessageAsync(Keys.OutputFruit2, msgbody);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML UpdateObjectAsync failed to send outputfruit2 {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }),
                    Task.Run(async () =>
                        {
                            try
                            {
                                Log.WriteLineVerbose("\t\t\t\t\t\tWinML UpdateObjectAsync to upstream kvp = {0}", kvp.ToString());
                                await Module.SendMessageAsync(Keys.OutputUpstream, msgbody);
                            }
                            catch (Exception e)
                            {
                                Log.WriteLineError("WinML UpdateObjectAsync failed to send outputfruit upstream {0}", e.ToString());
                                Environment.Exit(2);
                            }
                        })
                );
             }
        }
        public async Task NotifyNewModuleOfCurrentStateAsync()
        {
            byte[] fruit = null;
            lock (_lastFruitBody)
            {
                fruit = _lastFruitBody;
            }
            if (fruit != null && fruit.Length > 1)
            {
                Log.WriteLine("NotifyNewModuleOfCurrentStateAsync {0}", Encoding.UTF8.GetString(fruit));
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Module.SendMessageAsync(Keys.OutputFruit0, fruit);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML NotifyNewModuleOfCurrentStateAsync failed to send outputfruit0 {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Module.SendMessageAsync(Keys.OutputFruit1, fruit);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML NotifyNewModuleOfCurrentStateAsync failed to send outputfruit1 {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Module.SendMessageAsync(Keys.OutputFruit2, fruit);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML NotifyNewModuleOfCurrentStateAsync failed to send outputfruit2 {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Module.SendMessageAsync(Keys.OutputUpstream, fruit);
                        }
                        catch (Exception e)
                        {
                            Log.WriteLineError("WinML NotifyNewModuleOfCurrentStateAsync failed to send outputfruit upstream {0}", e.ToString());
                            Environment.Exit(2);
                        }
                    })
                );
            }
            else
            {
                Log.WriteLine("NotifyNewModuleOfCurrentStateAsync no current state yet");
            }
        }
    }
}
