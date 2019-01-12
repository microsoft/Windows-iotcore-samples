//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using EdgeModuleSamples.Messages;
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
    public class DeploymentConfig
    {
        private static async Task<YamlDocument> LoadConfigAsync(string configFile)
        {
            StreamReader sr = null;
            await Task.WhenAll(Task.Run(() => {
                sr = new StreamReader(configFile);
            }));
            var yamlString = await sr.ReadToEndAsync();
            YamlDocument yamlDoc = null;
            await Task.WhenAll(Task.Run(() => {
                yamlDoc = new YamlDocument(yamlString);
            }));
            return yamlDoc;
        }
        private static string GetDeviceConnectionStringInternal(YamlDocument doc)
        {
            return (string)doc.GetKeyValue("device_connection_string");
        }
        public static async Task<string> GetDeviceConnectionStringAsync()
        {
            string configRoot = System.Environment.GetEnvironmentVariable("ProgramData");
            configRoot += @"\iotedge";
            string configFileName = @"\config.yaml";
            string configPath = null;
            if (File.Exists(configRoot + configFileName)) {
                configPath = configRoot + configFileName;
            } else {
                configRoot = System.Environment.GetEnvironmentVariable("LocalAppData");
                configRoot += @"\iotedge";
                configPath = configRoot + configFileName;
            }

            Log.WriteLine("loading config file from {0}", configPath);
            var doc = await LoadConfigAsync(configPath);
            return GetDeviceConnectionStringInternal(doc);
        }
    }
    public class YamlDocument
    {
        readonly Dictionary<object, object> root;

        public YamlDocument(string input)
        {
            var reader = new StringReader(input);
            var deserializer = new Deserializer();
            this.root = (Dictionary<object, object>)deserializer.Deserialize(reader);
        }

        public object GetKeyValue(string key)
        {
            if (this.root.ContainsKey(key))
            {
                return this.root[key];
            }

            foreach (var item in this.root)
            {
                var subItem = item.Value as Dictionary<object, object>;
                if (subItem != null && subItem.ContainsKey(key))
                {
                    return subItem[key];
                }
            }

            return null;
        }
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

    class AzureModule
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
        private static void OnConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {

            Log.WriteLine("connection changed.  status {0} reason {1}", status, reason);
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
            string id = null;
            if (_moduleTwin == null)
            {
                Log.WriteLine("missing module twin -- unknown module id");
                id = "unknown";
            } else
            {
                id = _moduleTwin.ModuleId;
            }

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

        public AzureModule()
        {

        }
        public async Task AzureModuleInitAsync()
        {
            AmqpTransportSettings[] settings = new AmqpTransportSettings[2];
            settings[0] = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            settings[0].OpenTimeout = TimeSpan.FromSeconds(120);
            settings[0].OperationTimeout = TimeSpan.FromSeconds(120);
            settings[1] = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only);
            settings[1].OpenTimeout = TimeSpan.FromSeconds(120);
            settings[1].OperationTimeout = TimeSpan.FromSeconds(120);
            _moduleClient = null;
            //var start = DateTime.Now;
            // if multiple modules are starting simultaneously or ML is heavily loading the system it can take a while to establish a connection especially on low-end arm32
            //var default_timeout = TimeSpan.FromSeconds(120); 
            //while (_moduleClient == null)
            //{
            //try
            //{
                    _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
                //}
                //catch (IotHubException e)
                //{
                    //Log.WriteLine("suppressing IotHub Exception during module client creation {0}", e.ToString());
                    //if (DateTime.Now - start >= default_timeout)
                    //{
                        //throw e;  // rethrow eventually
                    //}
                    // ignore any hub errors for a while
                //}
            //}
            Log.WriteLine("ModuleClient Initialized");
            // TODO: connection status chnages handler
            bool openSucceeded = false;
            while (!openSucceeded)
            {
                //try
                //{
                    _moduleClient.SetConnectionStatusChangesHandler(OnConnectionChanged);
                    await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredModulePropertyChanged, this);
                    await _moduleClient.SetInputMessageHandlerAsync("inputfruit", OnFruitMessageReceived, this);
                    await _moduleClient.OpenAsync();
                    openSucceeded = true;
                //}
                //catch (IotHubException e)
                //{
                    //Log.WriteLine("suppressing IotHub Exception during module client open {0}", e.ToString());
                    //if (DateTime.Now - start >= default_timeout)
                    //{
                        //throw e;  // rethrow eventually
                    //}
                    // ignore any hub errors for a while
                //}
            }
            Log.WriteLine("ModuleClient Opened");
            _moduleTwin = await _moduleClient.GetTwinAsync();
            Log.WriteLine("ModuleTwin Retrieved");
            await OnDesiredModulePropertyChanged(_moduleTwin.Properties.Desired, this);
            Log.WriteLine("ModuleTwin Initial Desired Properties Processed");
            NotifyModuleLoad();
            Log.WriteLine("Module Load D2C message fired");
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

        private MessageHandler _inputMessageHandler { get; set; }
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
