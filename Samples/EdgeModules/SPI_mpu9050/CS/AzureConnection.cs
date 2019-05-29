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

namespace SPIMPU9050
{
    [JsonObject(MemberSerialization.Fields)]
    class ConfigurationType : SpbBaseConfigurationType
    {
        public override bool Update(BaseConfigurationType newValue)
        {
            // TODO: implement cloud side device configuration
            Log.WriteLine("updating from {0} to {1}", this.ToString(), newValue.ToString());
            return true;
        }
    }
    class AzureModule : AzureModuleBase
    {
        public event EventHandler<string> ModuleLoaded;

        private DesiredPropertiesType<ConfigurationType> _desiredProperties;
        public ConfigurationType Configuration { get { return _desiredProperties.Configuration; } }
        public event EventHandler<ConfigurationType> ConfigurationChanged;
        public override string ModuleId { get { return Keys.SPIModuleId; } }

        async Task<MessageResponse> ProcessModuleLoadedMessage(ModuleLoadedMessage msg)
        {
            await Task.Run(() => {
                try
                {
                    ModuleLoaded?.Invoke(this, msg.ModuleName);
                }
                catch (Exception e)
                {
                    Log.WriteLine("ModuleLoadedMessageHandler event lambda exception {0}", e.ToString());
                }
            });
            return MessageResponse.Completed;
        }
        static async Task<MessageResponse> ModuleLoadedMessageHandler(Message msg, object ctx)
        {
            try
            {
                AzureModule module = (AzureModule)ctx;
                var msgBytes = msg.GetBytes();
                var msgString = Encoding.UTF8.GetString(msgBytes);
                Log.WriteLine("loadModule msg received: '{0}'", msgString);
                var loadMsg = JsonConvert.DeserializeObject<ModuleLoadedMessage>(msgString);
                await module.ProcessModuleLoadedMessage(loadMsg);
                return MessageResponse.Completed;
            }
            catch (Exception e)
            {
                Log.WriteLine("Exception in ModuleLoadedMessageHandler: '{0}'", e.ToString());
            }
            Environment.Exit(3);
            return MessageResponse.Abandoned;
        }
        private async Task<MethodResponse> SetModuleLoaded(MethodRequest req, object context)
        {
            try
            {
                string data = Encoding.UTF8.GetString(req.Data);
                Log.WriteLine("Direct Method SetModuleLoaded {0}", data);
                var loadMsg = JsonConvert.DeserializeObject<ModuleLoadedMessage>(data);
                AzureModule module = (AzureModule)context;
                await module.ProcessModuleLoadedMessage(loadMsg);
                // Acknowlege the direct method call with a 200 success message
                string result = "{\"result\":\"Executed direct method: " + req.Name + "\"}";
                return new MethodResponse(Encoding.UTF8.GetBytes(result), 200);
            }
            catch (Exception e)
            {
                Log.WriteLine("Exception in SetModuleLoaded: '{0}'", e.ToString());
            }
            Environment.Exit(3);
            return null;
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
                await UpdateReportedPropertiesAsync(new KeyValuePair<string, object>(Keys.Configuration, JsonConvert.SerializeObject(_desiredProperties.Configuration)));

            }
            Log.WriteLine("update complete -- current properties {0}", _desiredProperties.ToString());
        }

        public AzureModule()
        {
        }
        public override async Task AzureModuleInitAsync<C>(C c) 
        {
            Log.WriteLine("derived AzureModuleInit");
            AzureConnection c1 = c as AzureConnection;
            await base.AzureModuleInitAsync(c1);
            await _moduleClient.SetInputMessageHandlerAsync(Keys.ModuleLoadedInputRoute, ModuleLoadedMessageHandler, this);
            await _moduleClient.SetMethodHandlerAsync(Keys.SetModuleLoaded, SetModuleLoaded, this);
            Log.WriteLine("derived Handlers Set");
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
            return await CreateAzureConnectionAsync<AzureConnection, AzureModule>();
        }

        public async Task NotifyNewModuleOfCurrentStateAsync()
        {
            byte[] obody = null;
            lock (_lastOBody)
            {
                obody = _lastOBody;
            }
            if (obody != null &&_lastOBody.Length > 1)
            {
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try
                        {
                            await Module.SendMessageAsync(Keys.OutputOrientation, obody);
                        } catch (Exception e)
                        {
                            Log.WriteLineError("SPI Exception sending current state to local0 {0}", e.ToString());
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try { 
                            await Module.SendMessageAsync(Keys.OutputUpstream, obody);
                        } catch (Exception e)
                        {
                            Log.WriteLineError("SPI Exception sending current state upstream {0}", e.ToString());
                        }
                    })
                );
            }
        }
        public override async Task UpdateObjectAsync(KeyValuePair<string, object> kvp)
        {
            Log.WriteLine("\t\t\t\t\t\tSPI UpdateObjectAsync override kvp = {0}", kvp.ToString());
            if (kvp.Key == Keys.Orientation)
            {
                // output the event stream
                var msgvalue = new OrientationMessage();
                msgvalue.OriginalEventUTCTime = DateTime.UtcNow.ToString("o");
                msgvalue.OrientationState = (Orientation)kvp.Value;
                byte[] msgbody = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(msgvalue));
                lock (_lastOBody)
                {
                    _lastOBody = msgbody;
                }
                await Task.WhenAll(
                    Task.Run(async () =>
                    {
                        try { 
                            await Module.SendMessageAsync(Keys.OutputOrientation, msgbody);
                        } catch (Exception e)
                        {
                            Log.WriteLineError("SPI Exception updating orientation local {0}", e.ToString());
                        }
                    }),
                    Task.Run(async () =>
                    {
                        try { 
                            var m = new Message(msgbody);
                            await Module.SendMessageAsync(Keys.OutputUpstream, msgbody);
                        } catch (Exception e)
                        {
                            Log.WriteLineError("SPI Exception updating orientation upstream {0}", e.ToString());
                        }
                    })
                );
            }
        }
    }
}
