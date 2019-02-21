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

namespace ConsoleDotNetCoreWinML
{
    public class AzureModule : AzureModuleBase {
        public event EventHandler<string> ModuleLoaded;

        static async Task<MessageResponse> ModuleLoadMessageHandler(Message msg, Object ctx)
        {
            AzureModule module = (AzureModule)ctx;
            var msgBytes = msg.GetBytes();
            var msgString = Encoding.UTF8.GetString(msgBytes);
            Log.WriteLine("loadModule msg received: '{0}'", msgString);
            var loadMsg = JsonConvert.DeserializeObject<ModuleLoadMessage>(msgString);
            await Task.Run(() => {
                try
                {
                    module.ModuleLoaded?.Invoke(module, loadMsg.ModuleName);
                } catch (Exception e)
                {
                    Log.WriteLine("ModuleLoadMessageHandler event lambda exception {0}", e.ToString());
                }
                });
            return MessageResponse.Completed;

        }
        public AzureModule() {
        }
        public override async Task AzureModuleInitAsync<C>(C c)
        {
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            await _moduleClient.SetInputMessageHandlerAsync(Keys.ModuleLoadInputRoute, ModuleLoadMessageHandler, this);
            await base.AzureModuleInitEndAsync();
        }

    };
    public class AzureDevice : AzureDeviceBase
    {
        public AzureDevice() { }
    };
    public class AzureConnection : AzureConnectionBase
    {
        private byte[] _lastFruitBody;
        public AzureConnection() {
            _lastFruitBody = new byte[0];
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync()
        {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public override async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\t\tWinML UpdateObjectAsync override kvp = {0}", kvp.ToString());
            if (kvp.Key == Keys.FruitSeen)
            {
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
                            var m = new Message(msgbody);
                            await Module.SendMessageAsync(Keys.OutputFruit, m);
                        }),
                    Task.Run(async () =>
                        {
                            var m = new Message(msgbody);
                            await Module.SendMessageAsync(Keys.OutputUpstream, m);
                        })
                );
             }
        }
        public async Task NotifyNewModuleAsync()
        {
            if (_lastFruitBody.Length > 1)
            {
                Message m = null;
                lock (_lastFruitBody)
                {
                    m = new Message(_lastFruitBody);
                }
                await Module.SendMessageAsync(Keys.OutputFruit, m);
            }
        }
    }
}
