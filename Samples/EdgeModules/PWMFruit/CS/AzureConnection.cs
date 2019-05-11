//
// Copyright (c) Microsoft. All rights reserved.
//

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

namespace PWMFruit
{
    [JsonObject(MemberSerialization.Fields)]
    class ConfigurationType : SpbBaseConfigurationType
    {
        public override bool Update(BaseConfigurationType newValue)
        {
            var v = (ConfigurationType)newValue;
            Log.WriteLine("updating from {0} to {1}", this.ToString(), v.ToString());
            // TODO: implement cloud side device configuration
            bool rc = false;
            //bool rc = GpioPins.Update(v.GpioPins);
            Log.WriteLine("{0} update to {1}", rc ? "did" : "did not", this.ToString());
            return rc;
        }
        public override string ToString()
        {
            return String.Format("{0}", GetType().Name);
        }

    }

    class AzureModule : AzureModuleBase
    {
        private DateTime _lastFruitUTC;
        private DesiredPropertiesType<ConfigurationType> _desiredProperties;
        public ConfigurationType Configuration { get { return _desiredProperties.Configuration; } }
        public event EventHandler<ConfigurationType> ConfigurationChanged;
        public event EventHandler<string> FruitChanged;
        public override string ModuleId { get { return Keys.PWMModuleId; } }

        private async Task ProcessFruitMessage(FruitMessage fruitMsg)
        {
            DateTime originalEventUTC = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
            if (fruitMsg.OriginalEventUTCTime != null)
            {
                originalEventUTC = DateTime.Parse(fruitMsg.OriginalEventUTCTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Log.WriteLine("processing fruit message. parsed msg time {0} from {1}", originalEventUTC.ToString("o"), fruitMsg.OriginalEventUTCTime);
            }
            else
            {
                Log.WriteLine("msg has no time.  using current {0}", originalEventUTC.ToString("o"));
            }
            lock (FruitChanged)
            {
                if (originalEventUTC >= _lastFruitUTC)
                {
                    Log.WriteLine("processing fruit message. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), _lastFruitUTC.ToString("o"));
                    FruitChanged?.Invoke(this, fruitMsg.FruitSeen);
                    _lastFruitUTC = originalEventUTC;
                }
                else
                {
                    Log.WriteLine("processing fruit message. ignoring stale message. original event UTC {0} prev {1}", originalEventUTC.ToString("o"), _lastFruitUTC.ToString("o"));
                }
            }
            await Task.CompletedTask;
            return;
        }

        private static async Task<MessageResponse> OnFruitMessageReceived(Message msg, object ctx)
        {
            AzureModule module = (AzureModule)ctx;
            var msgBytes = msg.GetBytes();
            var msgString = Encoding.UTF8.GetString(msgBytes);
            Log.WriteLine("fruit msg received: '{0}'", msgString);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(msgString);
            await module.ProcessFruitMessage(fruitMsg);
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
            if (!newDesiredProperties.Contains(Keys.Configuration))
            {
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
                await UpdateReportedPropertiesAsync(new KeyValuePair<string, Object>(Keys.Configuration, JsonConvert.SerializeObject(_desiredProperties.Configuration)));

            }
            if (newDesiredProperties.Contains(Keys.FruitTest))
            {
                var fruit = (string)((JValue)newDesiredProperties[Keys.FruitTest]).Value;
                Log.WriteLine("fruittest {0}", fruit != null ? fruit : "(null)");
                if (fruit != null)
                {
                    Log.WriteLine("setting fruit {0}", fruit);
                    FruitChanged?.Invoke(this, fruit);
                }
            }
            Log.WriteLine("update complete -- current properties {0}", _desiredProperties.ToString());
        }

        public AzureModule()
        {
            _lastFruitUTC = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        }
        public override async Task AzureModuleInitAsync<C>(C c) 
        {
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            await _moduleClient.SetInputMessageHandlerAsync(Keys.InputFruit, OnFruitMessageReceived, this);
            // note: pwm doesn't support direct method setfruit since it's always local to the ML module and shouldn't get called from the cloud function
            await base.AzureModuleInitEndAsync();
        }
    }


    class AzureConnection : AzureConnectionBase
    {
        //private MessageHandler _inputMessageHandler { get; set; }
        public AzureConnection()
        {
            
        }
        // private async Task<MessageResponse> OnInputMessageReceived(Message msg, object ctx)
        // {
        // 
        // }
        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureModule>();
        }

        public async Task NotifyModuleLoadAsync()
        {
            await Task.WhenAll(
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadedOutputRouteLocal0)),
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadedOutputRouteLocal1)),
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadedOutputRouteUpstream))
            );
            Log.WriteLine("derived Module Load D2C message fired");
        }
    }
}
