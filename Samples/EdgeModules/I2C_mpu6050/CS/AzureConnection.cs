//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using EdgeModuleSamples.Common.Azure;
using EdgeModuleSamples.Common.Logging;
using EdgeModuleSamples.Common.Messages;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace I2CMPU6050
{
    [JsonObject(MemberSerialization.Fields)]
    class ConfigurationType : BaseConfigurationType
    {
        public override bool Update(BaseConfigurationType newValue)
        {
            Log.WriteLine("updating from {0} to {1}", this.ToString(), newValue.ToString());
            return true;
        }
    }
    public class AzureDevice : AzureDeviceBase
    {
        public AzureDevice() { }
    }

    class AzureModule : AzureModuleBase
    {
        // TODO: move common config to basemodule
        private DesiredPropertiesType<ConfigurationType> _desiredProperties;
        public ConfigurationType Configuration { get { return _desiredProperties.Configuration; } }
        public event EventHandler<ConfigurationType> ConfigurationChanged;

        public override async Task OnConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            await base.OnConnectionChanged(status, reason);
            Log.WriteLine("derived connection changed.  status {0} reason {1}", status, reason);
            return;
        }
        protected override async Task OnDesiredModulePropertyChanged(TwinCollection newDesiredProperties)
        {
            Log.WriteLine("derived desired properties contains {0} properties", newDesiredProperties.Count);
            await base.OnDesiredModulePropertyChanged(newDesiredProperties);
            DesiredPropertiesType<ConfigurationType> dp;
            if (!newDesiredProperties.Contains(Keys.Configuration)) {
                Log.WriteLine("derived desired properties contains no configuration.  skipping...");
                return;
            }
            dp.Configuration = ((JObject)newDesiredProperties[Keys.Configuration]).ToObject<ConfigurationType>();
            Log.WriteLine("checking for update current desiredProperties {0} new dp {1}", _desiredProperties.ToString(), dp.ToString());
            var changed = _desiredProperties.Update(dp);
            if (changed) {
                Log.WriteLine("desired properties {0} different then current properties, notifying...", _desiredProperties.ToString());
                ConfigurationChanged?.Invoke(this, dp.Configuration);
                Log.WriteLine("local notification complete. updating reported properties to cloud twin");
                await UpdateReportedPropertiesAsync(new KeyValuePair<string, object>(Keys.Configuration, JsonConvert.SerializeObject(_desiredProperties.Configuration))).ConfigureAwait(false);

            }
            Log.WriteLine("update complete -- current properties {0}", _desiredProperties.ToString());
        }

        public AzureModule()
        {
        }
        public override async Task AzureModuleInitAsync<C>(C c) 
        {
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            //await _moduleClient.SetInputMessageHandlerAsync(Keys.InputFruit, OnFruitMessageReceived, this);
            //await _moduleClient.SetMethodHandlerAsync(Keys.SetFruit, SetFruit, this);
            await base.AzureModuleInitEndAsync();
        }
    }


    class AzureConnection : AzureConnectionBase
    {
        private byte[] _lastOBody;

            public AzureConnection()
        {
            _lastOBody = new byte[0];
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public async Task NotifyNewModuleAsync()
        {
                if (_lastOBody.Length > 1)
                {
                    Message m = null;
                    lock (_lastOBody)
                    {
                        m = new Message(_lastOBody);
                    }
                    await Module.SendMessageAsync(Keys.OutputOrientation, m);
                }
        }
        public override async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\t\tI2C UpdateObjectAsync override kvp = {0}", kvp.ToString());
            if (kvp.Key == Keys.Orientation)
            {
                // output the event stream
                var msgvalue = new OrientationMessage();
                msgvalue.OriginalEventUTCTime = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).ToString("o");
                msgvalue.OrientationState = (Orientation)kvp.Value;
                byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgvalue));
                lock (_lastOBody)
                {
                    _lastOBody = msgbody;
                }
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        var m = new Message(msgbody);
                        await Module.SendMessageAsync(Keys.OutputOrientation, m);
                    }),
                    Task.Run(async () =>
                    {
                        var m = new Message(msgbody);
                        await Module.SendMessageAsync(Keys.OutputUpstream, m);
                    })
                );
            }
        }
    }
}
