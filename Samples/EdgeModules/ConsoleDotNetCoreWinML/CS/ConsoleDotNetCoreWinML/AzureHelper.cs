//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using EdgeModuleSamples.Messages;
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
    class AzureConnection
    {
        private ConcurrentQueue<string> _updateq { get; set; }
        private TwinCollection _reportedProperties { get; set; }
        private ModuleClient _client { get; set; }
        private AzureConnection()
        {
            _updateq = new ConcurrentQueue<string>();
        }
        private async Task AzureConnectionInitAsync()
        {
            TransportType transport = TransportType.Amqp;
            _client = await ModuleClient.CreateFromEnvironmentAsync(transport);
            Log.WriteLine("Azure connection Initialized");
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync()
        {
            var newConnection = new AzureConnection();
            await newConnection.AzureConnectionInitAsync();
            await newConnection._client.OpenAsync();
            Log.WriteLine("Azure connection Creation Complete");
            return newConnection;
        }
        public void UpdateObject(string fruit)
        {
            _updateq.Enqueue(fruit);
            Task.Run(async () =>
            {
                try
                {
                    string f = null;
                    bool success = false;
                    while (!success && !_updateq.IsEmpty)
                    {
                        success = _updateq.TryDequeue(out f);
                        await UpdateObjectAsync(f);
                        var reporting = new TwinCollection
                        {
                            ["FruitSeen"] = fruit
                        };
                        await _client.UpdateReportedPropertiesAsync(reporting);
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLineError("Update failed {0}", e.ToString());
                }
            });
        }
        private async Task UpdateObjectAsync(string fruit)
        {
            // output the event stream
            var msgvalue = new FruitMessage();
            msgvalue.FruitSeen = fruit;
            byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgvalue));
            var m = new Message(msgbody);
            await _client.SendEventAsync("OutputFruit", m);
            // Update the module twin
        }
    }
}
