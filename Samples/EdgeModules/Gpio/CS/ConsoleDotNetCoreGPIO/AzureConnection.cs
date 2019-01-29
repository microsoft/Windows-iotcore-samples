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

namespace ConsoleDotNetCoreGPIO
{
    public class AzureDevice : AzureDeviceBase
    {
        public AzureDevice() { }
    }
    [JsonObject(MemberSerialization.Fields)]
    struct ConfigurationType
    {
        public GpioPinIndexesType GpioPins;
        public bool Update(ConfigurationType newValue)
        {
            Log.WriteLine("updating from {0} to {1}", this.ToString(), newValue.ToString());
            bool rc = GpioPins.Update(newValue.GpioPins);
            Log.WriteLine("{0} update to {1}", rc ? "did" : "did not", this.ToString());
            return rc;
        }
        public override string ToString()
        {
            return String.Format("{0} {1}", GetType().Name, GpioPins.ToString());
        }

    }
    struct DesiredPropertiesType
    {
        public ConfigurationType Configuration;
        public override string ToString()
        {
            return String.Format("{0} {1}", GetType().Name, Configuration.ToString());
        }
        public bool Update(DesiredPropertiesType newValue)
        {
            Log.WriteLine("updating from {0} to {1}", this.ToString(), newValue.ToString());
            bool rc = Configuration.Update(newValue.Configuration);
            Log.WriteLine("{0} update to {1}", rc ? "did" : "did not", this.ToString());
            return rc;
        }
    }

    class AzureModule : AzureModuleBase
    {
        private DateTime _lastFruitUTC;
        private DesiredPropertiesType _desiredProperties;
        public ConfigurationType Configuration { get { return _desiredProperties.Configuration; } }
        public event EventHandler<ConfigurationType> ConfigurationChanged;
        public event EventHandler<string> FruitChanged;
        private static async Task<MessageResponse> OnFruitMessageReceived(Message msg, object ctx)
        {
            AzureModule module = (AzureModule)ctx;
            var msgBytes = msg.GetBytes();
            var msgString = Encoding.UTF8.GetString(msgBytes);
            Log.WriteLine("fruit msg received: '{0}'", msgString);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(msgString);
            DateTime originalEventUTC = DateTime.UtcNow;
            if (fruitMsg.OriginalEventUTCTime != null)
            {
                originalEventUTC = DateTime.Parse(fruitMsg.OriginalEventUTCTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
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

        private Task<MethodResponse> SetFruit(MethodRequest req, Object context)
        {
            string data = Encoding.UTF8.GetString(req.Data);
            Log.WriteLine("Direct Method SetFruit {0}", data);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(data);
            DateTime originalEventUTC = DateTime.UtcNow;
            if (fruitMsg.OriginalEventUTCTime != null)
            {
                originalEventUTC = DateTime.Parse(fruitMsg.OriginalEventUTCTime, null, System.Globalization.DateTimeStyles.RoundtripKind);
                Log.WriteLine("SetFruit invoking event. parsed msg time {0} from {1}", originalEventUTC.ToString("o"), fruitMsg.OriginalEventUTCTime);
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

        public AzureModule()
        {
            _lastFruitUTC = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
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
            // TODO: connection status chnages handler
            //newConnection._inputMessageHandler += OnInputMessageReceived;
            //await newConnection._moduleClient.SetInputMessageHandlerAsync("????", _inputMessageHandler, newConnection)
            // Connect to the IoT hub using the MQTT protocol

            // Create a handler for the direct method call
            _deviceClient.SetMethodHandlerAsync("SetFruit", SetFruit, this).Wait();
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
        //private MessageHandler _inputMessageHandler { get; set; }
#if USE_DEVICE_TWIN
        private AzureDevice _device { get; set; }
#endif
        public AzureConnection()
        {
            
        }
        // private async Task<MessageResponse> OnInputMessageReceived(Message msg, object ctx)
        // {
        // 
        // }
        public static async Task<AzureConnection> CreateAzureConnectionAsync() {
            return await CreateAzureConnectionAsync<AzureConnection, AzureDevice, AzureModule>();
        }

        public async Task NotifyModuleLoad()
        {
            await NotifyModuleLoad(Keys.ModuleLoadOutputRouteLocal, Keys.GPIOModuleId);
            await NotifyModuleLoad(Keys.ModuleLoadOutputRouteUpstream, Keys.GPIOModuleId);
            Log.WriteLine("derived Module Load D2C message fired");
        }

#if DISABLED
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
#endif
    }
}
