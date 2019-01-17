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
        public AzureModule() { }
    };
    public class AzureDevice : AzureDeviceBase
    {
        public AzureDevice() { }
    };
    public class AzureConnection : AzureConnectionBase
    {
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
                await Module.SendMessageAsync(Keys.OutputFruit, m);
                // Update the module twin
            }
        }
    }
}
