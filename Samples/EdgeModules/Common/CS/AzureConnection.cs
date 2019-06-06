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
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace EdgeModuleSamples.Common.Azure
{
    abstract public class BaseConfigurationType
    {
        abstract public bool Update(BaseConfigurationType newValue);
    }

    abstract public class SpbBaseConfigurationType : BaseConfigurationType
    {
        public string DeviceName;
        abstract override public bool Update(BaseConfigurationType newValue);
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
    abstract public class AzureModuleBase : IDisposable
    {
        public virtual string ModuleId { get { throw new NotImplementedException(); } }
        WeakReference<AzureConnectionBase> _actualConnection;
        protected AzureConnectionBase _connection {
            get {
                AzureConnectionBase b = null;
                _actualConnection.TryGetTarget(out b);
                return b;
            }
            set
            {
                _actualConnection.SetTarget(value);
            }
        }
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
            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateReportedPropertiesAsync kvp = {0}", u.ToString());

            if (_reportedDeviceProperties.Contains(u.Key))
            {
                Log.WriteLine($"\t\t\t\t\tConnectionBase _reportedDeviceProperties contains key {u.Key}");
                var old = _reportedDeviceProperties;
                _reportedDeviceProperties = new TwinCollection();
                foreach (KeyValuePair<string, object> p in old)
                {
                    if (p.Key == u.Key)
                    {
                        _reportedDeviceProperties[u.Key] = u.Value;
                    } else
                    {
                        _reportedDeviceProperties[p.Key] = p.Value;
                    }
                }
            } else
            {
                Log.WriteLine($"\t\t\t\t\tConnectionBase _reportedDeviceProperties does not contain key {u.Key}");
                _reportedDeviceProperties[u.Key] = u.Value;
            }
            TwinCollection delta = new TwinCollection();
            delta[u.Key] = u.Value;
            Log.WriteLine("updating twin reported properties with key = {0}, vt = {1}:{2}", u.Key, u.Value.GetType(), u.Value.ToString());
            await _moduleClient.UpdateReportedPropertiesAsync(delta);

        }
        public async Task NotifyModuleLoadAsync(string route)
        {
            ModuleLoadedMessage msg = new ModuleLoadedMessage();
            string id = ModuleId;
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
            await SendMessageAsync(route, msgbody);
        }

        public async Task SendMessageAsync(string route, byte[] msg)
        {
            var original_UTC = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc).ToString("o");
            await AzureConnectionBase.RetryOperationAsync($"SendMessageAsync {route} {Encoding.UTF8.GetString(msg)}", async () =>
            {
                Message m = new Message(msg);  // Azure device client can't re-use messages
                m.Properties[Keys.MessageCreationUTC] = original_UTC;
                await _moduleClient.SendEventAsync(route, m);
            });
        }

        public AzureModuleBase()
        {
            _actualConnection = new WeakReference<AzureConnectionBase>(null);
        }
        ~AzureModuleBase()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_moduleClient != null)
                {
                    _moduleClient.Dispose();
                    _moduleClient = null;
                }
            }
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
            settings[0].OpenTimeout = TimeSpan.FromSeconds(AzureConnectionBase.DEFAULT_NETWORK_TIMEOUT);
            settings[0].OperationTimeout = TimeSpan.FromSeconds(AzureConnectionBase.DEFAULT_NETWORK_TIMEOUT);
            settings[1] = new AmqpTransportSettings(TransportType.Amqp_WebSocket_Only);
            settings[1].OpenTimeout = TimeSpan.FromSeconds(AzureConnectionBase.DEFAULT_NETWORK_TIMEOUT);
            settings[1].OperationTimeout = TimeSpan.FromSeconds(AzureConnectionBase.DEFAULT_NETWORK_TIMEOUT);
            _reportedDeviceProperties = new TwinCollection();
            _moduleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            Log.WriteLine("ModuleClient Initialized");

            // set default message handler to log unexpected requests and fail them immediately without
            // blocking queues waiting for timeouts
            await AzureConnectionBase.RetryOperationAsync("Initializing default message handler", async () =>
            {
                await _moduleClient.SetMessageHandlerAsync(async (Message msg, object ctx) =>
                {
                    try {
                        AzureModuleBase m = (AzureModuleBase)ctx;
                        Log.WriteLine("Unexpected Input Message to {0} in module {1}", msg.InputName, m.ModuleId);
                        Log.WriteLine("    {0}", Encoding.UTF8.GetString(msg.GetBytes()));
                        await Task.CompletedTask;
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureModuleBase Default MessageHandler lambda exception {0}", e.ToString());
                        Environment.Exit(1); // failfast
                    }
                    return MessageResponse.Abandoned;
                }, this);
                Log.WriteLine("AzureModuleBase default msg handler set");
            });
            // set default method handler to log unexpected requests and fail them immediately without
            // blocking queues waiting for timeouts
            await AzureConnectionBase.RetryOperationAsync("Initializing default method handler", async () =>
            { 
                await _moduleClient.SetMethodDefaultHandlerAsync(async (MethodRequest req, object ctx) => {
                    try {
                        AzureModuleBase m = (AzureModuleBase)ctx;
                        Log.WriteLine("Unexpected Method Request in module {0}", m.ModuleId);
                        Log.WriteLine("    {0}", req.ToString());
                        await Task.CompletedTask;
                        string result = "{\"result\":\"Unrecognized direct method: " + req.Name + "\"}";
                        return new MethodResponse(Encoding.UTF8.GetBytes(result), 404);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureModuleBase MethodDefaultHandler lambda exception {0}", e.ToString());
                        Environment.Exit(1); // failfast
                    }
                    return new MethodResponse(null, 404);
                }, this);
                Log.WriteLine("AzureModuleBase default method handler set");
            });
            await AzureConnectionBase.RetryOperationAsync("Set ConnectionStatusChangesHandler", async () =>
            {
                _moduleClient.SetConnectionStatusChangesHandler(async (ConnectionStatus status, ConnectionStatusChangeReason reason) =>
                {
                    try
                    {
                        await this.OnConnectionChanged(status, reason);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureModuleBase ConnectionStatusChangesHandler lambda exception {0}", e.ToString());
                        Environment.Exit(1); // failfast
                    }
                });
                Log.WriteLine("AzureModuleBase connectionstatus handler set");

                await Task.CompletedTask;
                return;
            });
            await AzureConnectionBase.RetryOperationAsync("Initializing SetDesiredPropertyUpdateCallback", async () =>
            {
                await _moduleClient.SetDesiredPropertyUpdateCallbackAsync(async (TwinCollection newDesiredProperties, object ctx) =>
                {
                    try
                    {
                        var module = (AzureModuleBase)ctx;
                        await module.OnDesiredModulePropertyChanged(newDesiredProperties);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureModuleBase DesiredPropertyUpdateCallback lambda exception {0}", e.ToString());
                        Environment.Exit(1); // failfast
                    }
                }, this);
                Log.WriteLine("AzureModuleBase desired property update handler set");
            });
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

    abstract public class AzureConnectionBase : IDisposable
    {
        public static readonly int DEFAULT_NETWORK_TIMEOUT = 120;
        public static readonly int MAXIMUM_AZURE_CONNECTION_NETWORK_RETRIES = 5;
        private ConcurrentQueue<KeyValuePair<string, object>> _updateq { get; set; }

        public virtual AzureModuleBase Module { get; protected set; }
        protected AzureConnectionBase()
        {
            _updateq = new ConcurrentQueue<KeyValuePair<string, object>>();
        }
        ~AzureConnectionBase()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Module != null)
                {
                    Module.Dispose();
                    Module = null;
                }
            }
        }

        public static async Task RetryOperationAsync(string description, Func<Task> a)
        {
            int retry = 0;
            for (; ; )
            {
                try
                {
                    await a.Invoke();
                    return;
                }
                catch (IotHubCommunicationException e)
                {
                    if (++retry > MAXIMUM_AZURE_CONNECTION_NETWORK_RETRIES)
                    {
                        Log.WriteLineError("IoTHubCommunicationException during Azure Connection Operation \"{0}\".", description);
                        Log.WriteLineError("\t\tretry count Exceeded. attempt abandoned \"{0}\". ", e.ToString());
                        Environment.Exit(2);
                    }
                    else
                    {
                        Log.WriteLine("IoTHubCommunicationException during Azure Connection Operation \"{0}\". retrying", description);
                        Log.WriteLineVerbose("\t\tIoTHubCommunicationException {0}", e.ToString());
                    }
                }
                catch (Exception e)
                {
                    Log.WriteLineError("Azure Connection Operation failed {0}", e.ToString());
                    Environment.Exit(2);
                }
            }
        }

    // private async Task<MessageResponse> OnInputMessageReceived(Message msg, object ctx)
    // {
    // 
    // }
    protected virtual async Task AzureConnectionInitAsync<M>() where M : AzureModuleBase, new()
        {
            await Task.Run(async () =>
                {
                    try
                    {
                        Module = new M();
                        await Module.AzureModuleInitAsync(this);
                    }
                    catch (Exception e)
                    {
                        Log.WriteLine("AzureConnectionInitAsync ModuleInit lambda exception {0}", e.ToString());
                        Environment.Exit(1); // failfast
                    }
                });
            Log.WriteLine("Azure base generic connection Initialized");
        }
        public static async Task<C> CreateAzureConnectionAsync<C, M>() where C : AzureConnectionBase, new() where M : AzureModuleBase, new()
        {
            var newConnection = new C();
            await newConnection.AzureConnectionInitAsync<M>();

            Log.WriteLine("Azure base generic connection Creation Complete");
            return newConnection;
        }
        protected async Task NotifyModuleLoadAsync(string route)
        {
            await Module.NotifyModuleLoadAsync(route);
            Log.WriteLine("Module Load D2C message fired module {0} route {1}", Module.ModuleId, route);
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
                        success = _updateq.TryDequeue(out u);
                        if (success)
                        {
                            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda kvp = {0}", u.ToString());
                            await UpdateObjectAsync(u);
                            Log.WriteLine("\t\t\t\t\tConnectionBase calling module updatereportedproperties kvp = {0}", u.ToString());
                            await Module.UpdateReportedPropertiesAsync(u);
                            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda complete");
                        }
                        else
                        {
                            Log.WriteLine("\t\t\t\t\tTryDequeue failed even though q isn't empty");
                        }
                    }
                } catch (Exception e)
                {
                    Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject lambda exception {0}", e.ToString());
                    Environment.Exit(1); // failfast
            }
            });
            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObject sync complete");
        }
        public virtual async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\tConnectionBase UpdateObjectAsync base -- no-op");
            await Task.CompletedTask;
        }
    }
}
