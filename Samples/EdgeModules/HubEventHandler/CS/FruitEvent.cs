//
// Copyright (c) Microsoft. All rights reserved.
//


using EdgeModuleSamples.Common.Messages;
using Microsoft.Azure.Devices;  
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EdgeHub;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Azure.EventHubs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using IoTHubTrigger = Microsoft.Azure.WebJobs.EventHubTriggerAttribute;
using D2CMessage = Microsoft.Azure.Devices.Client.Message;
using C2DMessage = Microsoft.Azure.Devices.Message;
using WebJobsExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;
namespace HubEventHandler
{
    public static class FruitEvent
    {
        static string[] SettableModules => new string[] {
            Keys.GPIOModuleId,
            Keys.UARTModuleId,
            Keys.PWMModuleId
        };
        public static string Dump(EventData d)
        {
            string r = Dump(d.SystemProperties);
            r += " ";
            r += Dump(d.Properties);
            r += " ";
            r += Encoding.UTF8.GetString(d.Body);
            return r;
        }
        public static string Dump(EventData.SystemPropertiesCollection d)
        {
            string r = "SystemProperties:";
            if (d == null)
            {
                r += "(null)";
            }
            else
            {
                foreach (var p in d)
                {
                    r += " k: " + p.Key.ToString() + " vt: " + p.Value.GetType().ToString() + " v: " + p.Value.ToString();
                }
            }
            return r;
        }
        public static string Dump(IDictionary<string, object> d)
        {
            string r = "MessageProperties:";
            if (d == null)
            {
                r += "(null)";
            }
            else
            {
                foreach (var p in d)
                {
                    r += " k: " + p.Key.ToString() + " vt: " + p.Value.GetType().ToString() + " v: " + p.Value.ToString();
                }
            }
            return r;
        }
        private static HttpClient client = new HttpClient();

        public static async Task<string> GetCurrentFruit(string deviceId, RegistryManager rm, ILogger log)
        {
            string fruit = null;

            // await twin = GetModuleTwinAsync(deviceId, moduleId);
            log.LogInformation($"finding master for {deviceId}");
            Twin selfTwin = await rm.GetTwinAsync(deviceId);
            var selfProps = selfTwin.Properties.Desired;
            var masterId = (string)selfProps[Keys.FruitMaster];
            log.LogInformation($"master device id {masterId} module id {Keys.FruitModuleId}");
            Twin masterTwin = await rm.GetTwinAsync(masterId, Keys.FruitModuleId);
            log.LogInformation("have master module twin");
            var masterProps = masterTwin.Properties.Reported;
            if (masterProps.Contains(Keys.FruitSeen))
            {
                fruit = masterProps[Keys.FruitSeen];
                log.LogInformation($"Current Fruit {fruit}");
            }
            else
            {
                log.LogInformation("master has not seen fruit");
            }
            return fruit;
        }
        public static async Task<string[]> GetFruitSlaves(string deviceId, RegistryManager rm, ILogger log)
        {
            List<string> slaves = new List<string>();

            log.LogInformation($"finding slaves for {deviceId}");
            Twin selfTwin = await rm.GetTwinAsync(deviceId);
            var selfProps = selfTwin.Properties.Desired;
            string slaveProps = selfProps[Keys.FruitSlaves].ToString();
            //log.LogInformation($"slave props {slaveProps}");

            Dictionary<string, string> d = JsonConvert.DeserializeObject<Dictionary<string, string>>(slaveProps);
            foreach (var kvp in d)
            {
                slaves.Add((string)kvp.Value);
            }
            return slaves.ToArray();
        }
        public static async Task C2DMessage(string connectionString, string deviceId, string moduleId, string fruit, string EventMsgTime, ILogger log)
        {
            if (fruit == null)
            {
                return; // nothing seen yet
            }
            ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString);
            log.LogInformation("have service client");
            FruitMessage fruitMsg = new FruitMessage();
            fruitMsg.FruitSeen = fruit;
            fruitMsg.OriginalEventUTCTime = EventMsgTime;
            var mi = new CloudToDeviceMethod("SetFruit");
            mi.ConnectionTimeout = TimeSpan.FromSeconds(10);
            mi.ResponseTimeout = TimeSpan.FromSeconds(120);
            mi.SetPayloadJson(JsonConvert.SerializeObject(fruitMsg));

            // Invoke the direct method asynchronously and get the response from the simulated device.
            log.LogInformation("invoking device method for {0} {1} with {2}", deviceId, moduleId, mi.ToString());
            var r = await client.InvokeDeviceMethodAsync(deviceId, moduleId, mi);
            log.LogInformation("device method invocation complete");

            return;
        }

        [FunctionName("ModuleLoadHub")]
        public static async Task HubRun([IoTHubTrigger("messages/events", Connection = "EndpointConnectionString")]EventData message, 
                                        ILogger log, WebJobsExecutionContext context)
        {
            try
            {
                string msg = Encoding.UTF8.GetString(message.Body);
                log.LogInformation("{0} C# IoT Hub trigger function received a message: {1} ::: {2}", Thread.CurrentThread.ManagedThreadId, Dump(message), message.ToString());
                log.LogInformation($"payload: {msg}");
                if (message.SystemProperties == null)
                {
                    log.LogInformation("no system metadata. unable to determine source device -- ignoring");
                    return;
                }
                if (message.Properties != null && 
                    message.Properties.ContainsKey(Keys.iothubMessageSchema) && 
                    (string)message.Properties[Keys.iothubMessageSchema] == Keys.twinChangeNotification)
                {
                    log.LogInformation("twin notification -- ignoring");
                    return;
                }
                string deviceName = (string)message.SystemProperties[Keys.DeviceIdMetadata];
                log.LogInformation($"Have device id {deviceName}");
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
                string conn = config[Keys.HubConnectionString];
                log.LogInformation("Have connection string {0}", conn);
                var rm = RegistryManager.CreateFromConnectionString(conn);
                log.LogInformation("Have registry manager");
                ModuleLoadMessage loadMsg = JsonConvert.DeserializeObject<ModuleLoadMessage>(msg);
                if (((IList<string>)SettableModules).Contains(loadMsg.ModuleName))
                {
                    log.LogInformation("Load Module {0}", loadMsg.ModuleName);
                    string fruit = await GetCurrentFruit(deviceName, rm, log);
                    log.LogInformation($"Have fruit {fruit}");

                    await C2DMessage(conn, deviceName, loadMsg.ModuleName, fruit, null, log);
                }
                else
                {
                    log.LogInformation("Not a Load Module -- checking for fruit");
                    FruitMessage fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(msg);
                    if (fruitMsg.FruitSeen != null)
                    {
                        log.LogInformation("fruit msg original time {0} fruit {1}", fruitMsg.OriginalEventUTCTime, fruitMsg.FruitSeen);
                        string[] slaves = await GetFruitSlaves(deviceName, rm, log);
                        //log.LogInformation("found {0} slave devices", slaves.Length);
                        string originaleventtime = fruitMsg.OriginalEventUTCTime;
                        if (originaleventtime == null && message.Properties.ContainsKey(Keys.MessageCreationUTC))
                        {
                            originaleventtime = (string)message.Properties[Keys.MessageCreationUTC];
                        }
                        foreach (var s in slaves)
                        {
                            log.LogInformation("sending fruit slave {0}  originalUTC {1} fruit {2}", s, originaleventtime == null ? "(null)" : originaleventtime, fruitMsg.FruitSeen);
                            try
                            {
                                foreach (var m in SettableModules)
                                {
                                    log.LogInformation("setting module {0}", m);
                                    await C2DMessage(conn, s, m, fruitMsg.FruitSeen, originaleventtime, log);
                                }
                            }
                            catch (Exception e)
                            {
                                log.LogInformation("{0} exception sending fruit slave {1} GPIO fruit {2}. ex = {3}", Thread.CurrentThread.ManagedThreadId, s, fruitMsg.FruitSeen, e.ToString());
                            }
                        }
                    }
                }
            } catch (Exception e)
            {
                log.LogInformation("{0} HubRun failed with exception {1}", Thread.CurrentThread.ManagedThreadId, e.ToString());
            }
            log.LogInformation("{0}HubRun complete", Thread.CurrentThread.ManagedThreadId);
            return;
        }
    }
}