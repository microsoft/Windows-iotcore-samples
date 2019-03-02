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
using YamlDotNet.Serialization;

namespace UARTLCD
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
        private DateTime _lastFruitUTC;
        private DesiredPropertiesType<ConfigurationType> _desiredProperties;
        public ConfigurationType Configuration { get { return _desiredProperties.Configuration; } }
        public event EventHandler<ConfigurationType> ConfigurationChanged;
        public event EventHandler<string> FruitChanged;

        // TODO: refactor setfruit and onfruitmessage to share common code
        private Task<MethodResponse> SetFruit(MethodRequest req, Object context)
        {
            string data = Encoding.UTF8.GetString(req.Data);
            Log.WriteLine("Direct Method SetFruit {0}", data);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(data);
            DateTime originalEventUTC = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            if (fruitMsg.OriginalEventUTCTime != null)
            {
                originalEventUTC = DateTime.Parse(fruitMsg.OriginalEventUTCTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Log.WriteLine("SetFruit invoking event. parsed msg time {0} from {1}", originalEventUTC.ToString("o"), fruitMsg.OriginalEventUTCTime);
            }
            else
            {
                Log.WriteLine("msg has no time.  using current {0}", originalEventUTC.ToString("o"));
            }
            if (originalEventUTC >= _lastFruitUTC)
            {
                Log.WriteLine("SetFruit invoking event. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), _lastFruitUTC.ToString("o"));
                AzureModule module = (AzureModule)context;
                module.FruitChanged?.Invoke(module, fruitMsg.FruitSeen);
                _lastFruitUTC = originalEventUTC;
            }
            else
            {
                Log.WriteLine("SetFruit ignoring stale message. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), _lastFruitUTC.ToString("o"));
            }
            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + req.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }
        private static async Task<MessageResponse> OnFruitMessageReceived(Message msg, object ctx)
        {
            AzureModule module = (AzureModule)ctx;
            var msgBytes = msg.GetBytes();
            var msgString = Encoding.UTF8.GetString(msgBytes);
            Log.WriteLine("fruit msg received: '{0}'", msgString);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(msgString);
            DateTime originalEventUTC = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            if (fruitMsg.OriginalEventUTCTime != null)
            {
                originalEventUTC = DateTime.Parse(fruitMsg.OriginalEventUTCTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
            }
            else
            {
                Log.WriteLine("msg has no time.  using current {0}", originalEventUTC.ToString("o"));
            }

            if (originalEventUTC >= module._lastFruitUTC)
            {
                Log.WriteLine("FruitMsgHandler invoking event. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), module._lastFruitUTC.ToString("o"));
                await Task.Run(() => module.FruitChanged?.Invoke(module, fruitMsg.FruitSeen));
                module._lastFruitUTC = originalEventUTC;
            }
            else
            {
                Log.WriteLine("FruitMsgHandler ignoring stale message. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), module._lastFruitUTC.ToString("o"));
            }
            return MessageResponse.Completed;
        }

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
            await _moduleClient.SetInputMessageHandlerAsync(Keys.InputFruit, OnFruitMessageReceived, this);
            await _moduleClient.SetMethodHandlerAsync(Keys.SetFruit, SetFruit, this);
            await base.AzureModuleInitEndAsync();
        }
    }


    class AzureConnection : AzureConnectionBase
    {
        public AzureConnection()
        {
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public async Task NotifyModuleLoadAsync()
        {
            await Task.WhenAll(
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadOutputRouteLocal0, Keys.GPIOModuleId)),
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadOutputRouteLocal1, Keys.GPIOModuleId)),
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadOutputRouteUpstream, Keys.GPIOModuleId))
            );
            Log.WriteLine("derived Module Load D2C message fired");
        }

    }
}
