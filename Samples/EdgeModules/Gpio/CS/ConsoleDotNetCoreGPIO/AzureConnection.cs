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
        private AzureConnection _connection { get; set; }
        private ModuleClient _moduleClient { get; set; }
        private Twin _moduleTwin { get; set; }
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
            await Task.Run(() =>module.FruitChanged?.Invoke(module, fruitMsg.FruitSeen));
            return MessageResponse.Completed;
        }
        public override async Task OnConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            await base.OnConnectionChanged(status, reason);
            Log.WriteLine("derived connection changed.  status {0} reason {1}", status, reason);
            return;
        }
        private static async Task OnDesiredModulePropertyChanged(TwinCollection newDesiredProperties, object ctx)
        {
            var module = (AzureModule)ctx;
            // TODO: process new properties
            Log.WriteLine("desired properties contains {0} properties", newDesiredProperties.Count);
            foreach (var p in newDesiredProperties)
            {
                Log.WriteLine("property {0}", p.GetType());
                var pv = (KeyValuePair<string, object>)p;
                Log.WriteLine("key = {0}, vt = {1}:{2}", pv.Key, pv.Value.GetType(), pv.Value);
            }
            DesiredPropertiesType dp;
            dp.Configuration = ((JObject)newDesiredProperties["Configuration"]).ToObject<ConfigurationType>();
            Log.WriteLine("checking for update current desiredProperties {0} new dp {1}", module._desiredProperties.ToString(), dp.ToString());
            var changed = module._desiredProperties.Update(dp);
            if (changed) {
                Log.WriteLine("desired properties {0} different then current properties, notifying...", module._desiredProperties.ToString());
                module.ConfigurationChanged?.Invoke(module, dp.Configuration);
                var rp = new TwinCollection();
                rp["Configuration"] = module._desiredProperties.Configuration;
                await module._moduleClient.UpdateReportedPropertiesAsync(rp).ConfigureAwait(false);
            }
            if (newDesiredProperties.Contains("FruitTest"))
            {
                var fruit = (string)((JValue)newDesiredProperties["FruitTest"]).Value;
                Log.WriteLine("fruittest {0}", fruit != null ? fruit : "(null)");
                if (fruit != null)
                {
                    Log.WriteLine("setting fruit {0}", fruit);
                    module.FruitChanged?.Invoke(module, fruit);
                }
            }
            Log.WriteLine("update complete -- current properties {0}", module._desiredProperties.ToString());
        }
        public void NotifyModuleLoad()
        {
            ModuleLoadMessage msg = new ModuleLoadMessage();
            string id = "GPIO";
            if (_moduleTwin == null)
            {
                Log.WriteLine("missing module twin -- assuming moduleId {0}", id);
                id = "unknown";
            } else
            {
                string tid = _moduleTwin.ModuleId;
                if (tid == null || tid.Length == 0)
                {
                    Log.WriteLine("missing module id -- assuming moduleId {0}", id);
                } else
                {
                    id = tid;
                }
            }
            Log.WriteLine("NotifyModuleLoad {0}", id);
            msg.ModuleName = id;

            NotifyMessage(msg);

        }
        public void NotifyMessage<T>(T msg)
        {
            Task.Run(async () =>
            {
                byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
                var m = new Message(msgbody);
                await _moduleClient.SendEventAsync("Output1", m);
            });
        }

        private Task<MethodResponse> SetFruit(MethodRequest req, Object context)
        {
            string data = Encoding.UTF8.GetString(req.Data);
            Log.WriteLine("Direct Method SetFruit {0}", data);
            var fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(data);
            AzureModule module = (AzureModule)context;
            module.FruitChanged?.Invoke(module, fruitMsg.FruitSeen);

            // Acknowlege the direct method call with a 200 success message
            string result = "{\"result\":\"Executed direct method: " + req.Name + "\"}";
            return Task.FromResult(new MethodResponse(Encoding.UTF8.GetBytes(result), 200));
        }

        public AzureModule()
        {

        }
        public override async Task AzureModuleInitAsync()
        {
            await base.AzureModuleInitBeginAsync();
            await _moduleClient.SetInputMessageHandlerAsync("inputfruit", OnFruitMessageReceived, this);
            await _moduleClient.SetMethodHandlerAsync("SetFruit", SetFruit, this);
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

    class AzureConnection
    {
        private ConcurrentQueue<string> _updateq { get; set; }

        //private MessageHandler _inputMessageHandler { get; set; }
        public AzureModule Module { get; private set; }
#if USE_DEVICE_TWIN
        private AzureDevice _device { get; set; }
#endif
        private AzureConnection()
        {
            _updateq = new ConcurrentQueue<string>();
        }
        // private async Task<MessageResponse> OnInputMessageReceived(Message msg, object ctx)
        // {
        // 
        // }
        private async Task AzureConnectionInitAsync()
        {
            await Task.WhenAll(
#if USE_DEVICE_TWIN

                // ignore twin until 
                Task.Run(async () => {
                    _device = new AzureDevice();
                    await _device.AzureDeviceInitAsync();
                }),
#endif
                Task.Run(async () =>
                {
                    Module = new AzureModule();
                    await Module.AzureModuleInitAsync();
                })
            );
            Log.WriteLine("Azure connection Initialized");
        }
        public static async Task<AzureConnection> CreateAzureConnectionAsync()
        {
            var newConnection = new AzureConnection();
            await newConnection.AzureConnectionInitAsync();

            Log.WriteLine("Azure connection Creation Complete");
            return newConnection;
        }
        public void NotifyModuleLoad()
        {
            Module.NotifyModuleLoad();
            Log.WriteLine("Module Load D2C message fired");
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
