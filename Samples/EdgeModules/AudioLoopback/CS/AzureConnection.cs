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

namespace AudioLoopback
{

    class ConfigurationType : BaseConfigurationType
    {
        public override bool Update(BaseConfigurationType newValue)
        {
            var v = (ConfigurationType)newValue;
            Log.WriteLine("updating from {0} to {1}", this.ToString(), v.ToString());
            return false;
        }
        public override string ToString()
        {
            return String.Format("{0} {1}", GetType().Name, base.ToString());
        }

    }
    class AzureModule : AzureModuleBase
    {
        private DesiredPropertiesType<ConfigurationType> _desiredProperties;
        public override string ModuleId { get { return Keys.AudioLoopbackModuleId; } }

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
            Log.WriteLine("update complete -- current properties {0}", _desiredProperties.ToString());
        }

        public AzureModule()
        {
        }
        public override async Task AzureModuleInitAsync<C>(C c) 
        {
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            await base.AzureModuleInitEndAsync();
        }
    }


    class AzureConnection : AzureConnectionBase
    {
        //private MessageHandler _inputMessageHandler { get; set; }
        public AzureConnection()
        {
            
        }

        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureModule>();
        }

        public async Task NotifyModuleLoadAsync()
        {
            await Task.WhenAll(
                Task.Run(async () => await NotifyModuleLoadAsync(Keys.ModuleLoadedOutputRouteUpstream))
            );
            Log.WriteLine("derived Module Load D2C message fired");
        }
    }
}
