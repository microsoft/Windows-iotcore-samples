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
using YamlDotNet.Serialization;

namespace ConsoleDotNetCoreI2c
{
    public class AzureDevice : AzureDeviceBase
    {
        public AzureDevice() { }
    }

    class AzureModule : AzureModuleBase
    {
        public override async Task OnConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            await base.OnConnectionChanged(status, reason);
            Log.WriteLine("derived connection changed.  status {0} reason {1}", status, reason);
            return;
        }
        protected override async Task OnDesiredModulePropertyChanged(TwinCollection newDesiredProperties)
        {
#if DISABLED
            // TODO: process new properties
            Log.WriteLine("derived desired properties contains {0} properties", newDesiredProperties.Count);
            await base.OnDesiredModulePropertyChanged(newDesiredProperties);
            DesiredPropertiesType dp;
            dp.Configuration = ((JObject)newDesiredProperties[Keys.Configuration]).ToObject<ConfigurationType>();
            Log.WriteLine("checking for update current desiredProperties {0} new dp {1}", _desiredProperties.ToString(), dp.ToString());
            var changed = _desiredProperties.Update(dp);
            if (changed) {
                Log.WriteLine("desired properties {0} different then current properties, notifying...", _desiredProperties.ToString());
                ConfigurationChanged?.Invoke(this, dp.Configuration);
                Log.WriteLine("local notification complete. updating reported properties to cloud twin");
                await UpdateReportedPropertiesAsync(new KeyValuePair<string, string>(Keys.Configuration, JsonConvert.SerializeObject(_desiredProperties.Configuration))).ConfigureAwait(false);

            }
            Log.WriteLine("update complete -- current properties {0}", _desiredProperties.ToString());
#else
            await Task.CompletedTask;
            return;
#endif

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

#if USE_DEVICE_TWIN
    class AzureDevice
    {
        private AzureConnection _connection { get; set; }
        private DeviceClient _deviceClient { get; set; }

        private Twin _deviceTwin { get; set; }
        private TwinCollection _reportedDeviceProperties { get; set; }
        private static async Task OnDesiredDevicePropertyChanged(TwinCollection desiredProperties, object ctx)
        {
            var device = (AzureDevice)ctx;
            Log.WriteLine("desired properties contains {0} properties", desiredProperties.Count);
            foreach (var p in desiredProperties)
            {
                Log.WriteLine("property {0}:{1}", p != null ? p.GetType().ToString() : "(null)", p != null ? p.ToString() : "(null)");
            }
            // TODO: compute delta and only send changes
            await device._deviceClient.UpdateReportedPropertiesAsync(device._reportedDeviceProperties).ConfigureAwait(false);
        }
    public AzureDevice() {
        }
        public async Task AzureDeviceInitAsync() {
            TransportType transport = TransportType.Amqp;
            _deviceClient = DeviceClient.CreateFromConnectionString(await DeploymentConfig.GetDeviceConnectionStringAsync(), transport);
            // TODO: connection status changes handler
            //newConnection._inputMessageHandler += OnInputMessageReceived;
            //await newConnection._moduleClient.SetInputMessageHandlerAsync("????", _inputMessageHandler, newConnection)
            // Connect to the IoT hub using the MQTT protocol

            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredDevicePropertyChanged, this);
            await _deviceClient.OpenAsync();
            Log.WriteLine("DeviceClient Initialized");
            var _deviceTwin = await _deviceClient.GetTwinAsync();
            Log.WriteLine("DeviceTwin Retrieved");
        }
    }
#endif

    class AzureConnection : AzureConnectionBase
    {
#if USE_DEVICE_TWIN
        private AzureDevice _device { get; set; }
#endif
        public AzureConnection()
        {
            
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public async Task NotifyModuleLoad()
        {
            await NotifyModuleLoad(Keys.ModuleLoadOutputRouteLocal, Keys.I2cModuleId);
            await NotifyModuleLoad(Keys.ModuleLoadOutputRouteUpstream, Keys.I2cModuleId);
            Log.WriteLine("derived Module Load D2C message fired");
        }

    }
}
