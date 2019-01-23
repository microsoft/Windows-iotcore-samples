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
            await Task.Run(() => module.ModuleLoaded?.Invoke(module, loadMsg.ModuleName));
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
        private Message _lastFruitMsg;
        public AzureConnection() { }
        public static async Task<AzureConnection> CreateAzureConnectionAsync()
        {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public override async Task UpdateObjectAsync(KeyValuePair<string, string> kvp)
        {
            if (kvp.Key == Keys.FruitSeen)
            {
                // output the event stream
                var msgvalue = new FruitMessage();
                msgvalue.FruitSeen = kvp.Value;
                byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgvalue));
                var m = new Message(msgbody);
                lock (_lastFruitMsg)
                {
                    _lastFruitMsg = m;
                }
                await Module.SendMessageAsync(Keys.OutputFruit, m);
            }
        }
        public async Task NotifyNewModuleAsync()
        {
            Message m = null;
            lock (_lastFruitMsg)
            {
                m = _lastFruitMsg;
            }
            await Module.SendMessageAsync(Keys.OutputFruit, m);
        }
    }
}
