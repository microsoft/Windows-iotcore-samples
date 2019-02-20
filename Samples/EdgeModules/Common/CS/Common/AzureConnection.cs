//
// Copyright (c) Microsoft. All rights reserved.
//

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

namespace EdgeModuleSamples.Common.Azure
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

    abstract public class BaseConfigurationType
    {
        abstract public bool Update(BaseConfigurationType newValue);
    }

    public struct DesiredPropertiesType<CONFIGURATIONTYPE> where CONFIGURATIONTYPE : BaseConfigurationType
{
        public CONFIGURATIONTYPE Configuration;
        public override string ToString()
        {
            return String.Format("{0} {1}", GetType().Name, (Configuration != null) ? Configuration.ToString() : "(null)");
        }
        public bool Update(DesiredPropertiesType<CONFIGURATIONTYPE> newValue)
        {
            Log.WriteLine("updating from {0} to {1}", this.ToString(), newValue.ToString());
            bool rc = true;
            if (Configuration == null)
            {
                Configuration = newValue.Configuration;
            }
            else
            {
                rc = Configuration.Update(newValue.Configuration);
            }
            Log.WriteLine("{0} update to {1}", rc ? "did" : "did not", this.ToString());
            return rc;
        }
    }
    abstract public class AzureModuleBase
    {
        protected AzureConnectionBase _connection { get; set; }
        protected ModuleClient _moduleClient { get; set; }
        protected Twin _moduleTwin { get; set; }
        private TwinCollection _reportedDeviceProperties { get; set; }
        public ConnectionStatus Status { get; set; }
        public ConnectionStatusChangeReason LastConnectionChangeReason { get; set; }
        public virtual async Task OnConnectionChanged(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            Log.WriteLine("base connection changed.  status {0} reason {1}", status, reason);
            Status = status;
            LastConnectionChangeReason = reason;
            await Task.CompletedTask;
        }
        protected virtual async Task OnDesiredModulePropertyChanged(TwinCollection newDesiredProperties)
        {
            // TODO: process new properties
            Log.WriteLine("base desired properties contains {0} properties", newDesiredProperties.Count);
            foreach (var p in newDesiredProperties)
            {
                Log.WriteLine("property {0}", p.GetType());
                var pv = (KeyValuePair<string, object>)p;
                Log.WriteLine("key = {0}, vt = {1}:{2}", pv.Key, pv.Value.GetType(), pv.Value.ToString());
            }
            await Task.CompletedTask;
        }
        public virtual async Task UpdateReportedPropertiesAsync(KeyValuePair<string, object> u)
        {
            _reportedDeviceProperties[u.Key] = u.Value.ToString();
            TwinCollection delta = new TwinCollection();
            delta[u.Key] = u.Value;
            Log.WriteLine("updating twin reported properties with key = {0}, vt = {1}:{2}", u.Key, u.Value.GetType(), u.Value.ToString());
            await _moduleClient.UpdateReportedPropertiesAsync(delta).ConfigureAwait(false);

        }
        public async Task NotifyModuleLoadAsync(string route, string defaultId)
        {
            ModuleLoadMessage msg = new ModuleLoadMessage();
            string id = defaultId;
            if (_moduleTwin == null)
            {
                Log.WriteLine("missing module twin -- assuming moduleId {0}", id);
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
            Log.WriteLine("base NotifyModuleLoadAsync {0} to {1}", id, route);
            msg.ModuleName = id;

            await SendMessageDataAsync(route, msg);
        }
        public async Task SendMessageDataAsync<T>(string route, T msg) where T : AzureMessageBase
        {
            byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msg));
            var m = new Message(msgbody);
            await SendMessageAsync(route, m);
        }

        public async Task SendMessageAsync(string route, Message msg)
        {
            msg.Properties[Keys.MessageCreationUTC] = DateTime.UtcNow.ToString("o");
            await _moduleClient.SendEventAsync(route, msg);
        }

        public AzureModuleBase()
        {
        }
        public virtual async Task AzureModuleInitAsync<C>(C c) where C : AzureConnectionBase
        {
            _connection = c;
            await AzureModuleInitBeginAsync();
        }
        //NOTE: actual work for module init split in 2 parts to allow derived classes to attach additional
        // message handlers prior to the module client open
        private async Task AzureModuleInitBeginAsync()
        {
            AmqpTransportSettings[] settings = new AmqpTransportSettings[2];
            settings[0] = new AmqpTransportSettings(TransportType.Amqp_Tcp_Only);
            settings[0].OpenTimeout = TimeSpan.FromSeconds(120);
            settings[0].OperationTimeout = TimeSpan.FromSeconds(120);
            settings[1] = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only);
            settings[1].OpenTimeout = TimeSpan.FromSeconds(120);
            settings[1].OperationTimeout = TimeSpan.FromSeconds(120);
            _reportedDeviceProperties = new TwinCollection();
            _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            Log.WriteLine("ModuleClient Initialized");

            _moduleClient.SetConnectionStatusChangesHandler(async (ConnectionStatus status, ConnectionStatusChangeReason reason) => { await this.OnConnectionChanged(status, reason); });
            //await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(OnDesiredModulePropertyChanged, this);
            await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection newDesiredProperties, object ctx) => 
            {
                var module = (AzureModuleBase)ctx;
                await module.OnDesiredModulePropertyChanged(newDesiredProperties);
            }, this);
        }
        protected async Task AzureModuleInitEndAsync()
        {
            await _moduleClient.OpenAsync();

            Log.WriteLine("ModuleClient Opened");
            _moduleTwin = await _moduleClient.GetTwinAsync();
            Log.WriteLine("ModuleTwin Retrieved");
            await OnDesiredModulePropertyChanged(_moduleTwin.Properties.Desired);
            Log.WriteLine("ModuleTwin Initial Desired Properties Processed");
        }
    }

#if USE_DEVICE_TWIN
    abstract public class AzureDeviceBase
    {
        private AzureConnectionBase _connection { get; set; }
        private DeviceClient _deviceClient { get; set; }

        private Twin _deviceTwin { get; set; }
        private TwinCollection _reportedDeviceProperties { get; set; }
        public virtual async Task OnDesiredDevicePropertyChanged(TwinCollection desiredProperties)
        {
            Log.WriteLine("desired properties contains {0} properties", desiredProperties.Count);
            foreach (var p in desiredProperties)
            {
                Log.WriteLine("property {0}:{1}", p != null ? p.GetType().ToString() : "(null)", p != null ? p.ToString() : "(null)");
            }
            // TODO: compute delta and only send changes
            await _deviceClient.UpdateReportedPropertiesAsync(_reportedDeviceProperties).ConfigureAwait(false);
        }
        private static async Task DesiredDevicePropertyChangedHandler(TwinCollection desiredProperties, object ctx)
        {
            var device = (AzureDeviceBase)ctx;
            await device.OnDesiredDevicePropertyChanged(desiredProperties);
            return;
        }

        public AzureDeviceBase()
        {
        }
        public async Task AzureDeviceInitAsync<C>(C c) where C : AzureConnectionBase {
            _connection = c;
            TransportType transport = TransportType.Amqp;
            _deviceClient = DeviceClient.CreateFromConnectionString(await DeploymentConfig.GetDeviceConnectionStringAsync(), transport);
            // TODO: connection status chnages handler
            //newConnection._inputMessageHandler += OnInputMessageReceived;
            //await newConnection._moduleClient.SetInputMessageHandlerAsync("????", _inputMessageHandler, newConnection)
            // Connect to the IoT hub using the MQTT protocol

            // Create a handler for the direct method call
            await _deviceClient.SetDesiredPropertyUpdateCallbackAsync(DesiredDevicePropertyChangedHandler, this);
            await _deviceClient.OpenAsync();
            Log.WriteLine("DeviceClient Initialized");
            var _deviceTwin = await _deviceClient.GetTwinAsync();
            Log.WriteLine("DeviceTwin Retrieved");
        }
    }
#else
    abstract public class AzureDeviceBase
    {
        private AzureConnectionBase _connection { get; set; }

        public AzureDeviceBase()
        {
        }
        public async Task AzureDeviceInitAsync<C>(C c) where C : AzureConnectionBase
        {
            _connection = c;
            await Task.CompletedTask;
        }
    }
#endif

    abstract public class AzureConnectionBase
    {
        private ConcurrentQueue<KeyValuePair<string, object>> _updateq { get; set; }

        public virtual AzureModuleBase Module { get; protected set; }
        public virtual AzureDeviceBase Device { get; protected set; }
        protected AzureConnectionBase()
        {
            _updateq = new ConcurrentQueue<KeyValuePair<string, object>>();
        }
        // private async Task<MessageResponse> OnInputMessageReceived(Message msg, object ctx)
        // {
        // 
        // }
        protected virtual async Task AzureConnectionInitAsync<D, M>() where D : AzureDeviceBase, new() where M : AzureModuleBase, new()
        {
            await Task.WhenAll(

                // ignore twin until 
                Task.Run(async () => {
                    try
                    {
                        Device = new D();
                        await Device.AzureDeviceInitAsync(this);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureConnectionInitAsync DeviceInit lambda exception {0}", e.ToString());
                    }
                }),

                Task.Run(async () =>
                {
                    try
                    {
                        Module = new M();
                        await Module.AzureModuleInitAsync(this);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureConnectionInitAsync ModuleInit lambda exception {0}", e.ToString());
                    }
                })
            );
            Log.WriteLine("Azure connection Initialized");
        }
        public static async Task<C> CreateAzureConnectionAsync<C, D, M>() where C : AzureConnectionBase, new() where D : AzureDeviceBase, new() where M : AzureModuleBase, new()
        {
            var newConnection = new C();
            await newConnection.AzureConnectionInitAsync<D, M>();

            Log.WriteLine("Azure connection Creation Complete");
            return newConnection;
        }
        protected async Task NotifyModuleLoadAsync(string route, string defaultId)
        {
            await Module.NotifyModuleLoadAsync(route, defaultId);
            Log.WriteLine("Module Load D2C message fired module {0} route {1}", defaultId, route);
        }


        public void UpdateObject(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject sync start");
            _updateq.Enqueue(kvp);
            Task.Run(async() =>
            {
                try
                {
                    KeyValuePair<string, object> u;
                    bool success = false;
                    while (!_updateq.IsEmpty)
                    {
                        do
                        {
                            success = _updateq.TryDequeue(out u);
                            if (success)
                            {
                                Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda kvp = {0}", u.ToString());
                                await UpdateObjectAsync(u);
                                Log.WriteLine("\t\t\t\t\tConnectionBase calling module updatereportedproperties kvp = {0}", u.ToString());
                                await Module.UpdateReportedPropertiesAsync(u);
                            }
                        } while (!success);
                        Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda complete");
                    }
                } catch (Exception e)
                {
                    Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda exception {0}", e.ToString());
                }
            });
            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject sync complete");
        }
        public virtual async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            await Task.CompletedTask;
        }
    }
}
