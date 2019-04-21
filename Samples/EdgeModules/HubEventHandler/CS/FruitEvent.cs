//
// Copyright (c) Microsoft. All rights reserved.
//


using EdgeModuleSamples.Common;
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
using System.Linq;
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
    using TopologyType = Tuple<Tuple<string, Twin>, Tuple<string, Twin>[]>;

    public static class FruitEvent
    {
        static readonly int DEFAULT_NETWORK_TIMEOUT = 300;

        //
        // currently this is setup to expect the following
        // device1: master
        //     modules: winml, gpio, spi, uart, pwm
        // device2: slave
        //     modules: gpio, uart, i2c
        //
        // i2c sends orientation to both uarts
        // spi sends orientation to both gpio
        // winml sends fruit to both gpio, both uart, pwm
        // gpio sends loadmodule to winml spi
        // uart sends loadmodule to winml i2c
        // pwm sends loadmodule to winml
        //
        // but local cross module traffic is handled by deployment routing 
        // in the local hub and never reaches this module
        //
        //  multi-slave is coded for but not tested
        //

        static Tuple<string, string[]>[] LoadMsgRouting => new Tuple<string, string[]>[] {
            new Tuple<string, string[]> ( 
                Keys.GPIOModuleId,
                new string[] {Keys.WinMLModuleId, Keys.SPIModuleId }
            ),
            new Tuple<string, string[]> (
                Keys.UARTModuleId,  
                new string[] {Keys.WinMLModuleId, Keys.I2CModuleId }
            ),
            new Tuple<string, string[]>(
                Keys.PWMModuleId, 
                new string[] {Keys.WinMLModuleId}
            )
        };
        static Tuple<string, string[]>[] FruitMsgRouting => new Tuple<string, string[]>[] {
            new Tuple<string, string[]>(
                Keys.WinMLModuleId,
                new string[] {Keys.GPIOModuleId, Keys.UARTModuleId, Keys.PWMModuleId}
            )
        };
        static Tuple<string, string[]>[] OrientationMsgRouting => new Tuple<string, string[]>[] {
            new Tuple<string, string[]>(
                Keys.I2CModuleId,
                new string[] {Keys.UARTModuleId}
            ),
            new Tuple<string, string[]>(
                Keys.SPIModuleId,
                new string[] {Keys.GPIOModuleId}
            )
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

        public static async Task<List<string>> GetDeviceModulesAsync(string deviceId, RegistryManager rm, ILogger log)
        {
            List<string> destCandidateNames = new List<string>();
            var deviceModules = await rm.GetModulesOnDeviceAsync(deviceId);
            log.LogInformation("destination {0} has {1} modules", deviceId, deviceModules.Count());
            //var destModuleCandidates = deviceModules.Where(module => module.Id != sourceModuleId);
            //log.LogInformation("destination {0} has {1} module candidates", destDeviceId, destModuleCandidates.Count());
            //var destCandidateNames = destModuleCandidates.Select(m => m.Id);
            destCandidateNames = deviceModules.Select(m => m.Id).ToList<string>();
            return destCandidateNames;
        }
        public static async Task<TopologyType> GetDeviceTopologyAsync(string deviceId, RegistryManager rm, ILogger log)
        {
            List<Tuple<string, Twin>> slaves = new List<Tuple<string, Twin>>();

            log.LogInformation($"finding slaves for {deviceId}");
            Twin selfTwin = await rm.GetTwinAsync(deviceId);
            var selfProps = selfTwin.Properties.Desired;
            var slaveProps = selfProps[Keys.FruitSlaves];
            Tuple<string, Twin> master = null;
            if (deviceId == selfProps[Keys.FruitMaster].ToString())
            {
                master = new Tuple<string, Twin>(deviceId, selfTwin);
            }
            else
            {
                string masterId = selfProps[Keys.FruitMaster].ToString();
                Twin masterTwin = await rm.GetTwinAsync(masterId);
                master = new Tuple<string, Twin>(masterId, masterTwin);
            }

            string slavejson = slaveProps.ToJson();
            log.LogInformation("Deserializing slave json {0}", slavejson);
            Dictionary<string, string> d = JsonConvert.DeserializeObject<Dictionary<string, string>>(slavejson);
            log.LogInformation("Deserialized {0} slave names", d.Count);
            foreach (var kvp in d)
            {
                string slaveName = (string)kvp.Value;
                Twin slaveTwin = null;
                if (slaveName != deviceId)
                {
                    slaveTwin = await rm.GetTwinAsync(slaveName);
                } else
                {
                    slaveTwin = selfTwin;
                }
                slaves.Add(new Tuple<string, Twin>(slaveName, slaveTwin));
            }
            log.LogInformation("adding {0} slaves to topology", slaves.Count);

            return new Tuple<Tuple<string, Twin>, Tuple<string, Twin>[]>(master, slaves.ToArray());
        }
        public static string[] GetDestinationDevicesFromTopology(string sourceDeviceId, TopologyType topology, ILogger log)
        {
            List<string> results = new List<string>();
            if (sourceDeviceId != topology.Item1.Item1)
            {
                var s = topology.Item1.Item1;
                log.LogInformation("source is not master. adding master {0} to dest list", s);
                results = results.Append(s).ToList<string>();
            } else
            {
                log.LogInformation("source is master. ignoring master");
            }
            //var slaves = topology.Item2.Where(s => s.Item1 != sourceDeviceId).Select(s => results.Append(s.Item1));
            var slavelist = topology.Item2.Where(s => s.Item1 != sourceDeviceId);
            log.LogInformation("found {0} slave topo entries", slavelist.Count());
            var slavenames = slavelist.Select(s => s.Item1);
            log.LogInformation("found {0} slavenames", slavenames.Count());
            results = results.Concat(slavenames).ToList<string>();
            //foreach (string s in slavenames)
            //{
            //    results.Add(s);
            //}
            log.LogInformation("found {0} destination device", results.Count);
            return results.ToArray();
        }
        public static async Task<string[]> GetDestinationDevices(string sourceDeviceId, RegistryManager rm, ILogger log)
        {
            var topology = await GetDeviceTopologyAsync(sourceDeviceId, rm, log);
            var results =  GetDestinationDevicesFromTopology(sourceDeviceId, topology, log);
            return results;
        }

        public static async Task C2DModuleMessage(string connectionString, string destDeviceId, string destModule, string sourceModuleId, ILogger log)
        {
            ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString);
            log.LogInformation("have service client");
            ModuleLoadedMessage loadMsg = new ModuleLoadedMessage();
            loadMsg.ModuleName = sourceModuleId;
            var mi = new CloudToDeviceMethod(Keys.SetModuleLoaded);
            mi.ConnectionTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.ResponseTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.SetPayloadJson(JsonConvert.SerializeObject(loadMsg));

            // Invoke the direct method asynchronously and get the response from the simulated device.
            log.LogInformation("invoking device method for {0} {1} with {2} json {3}", destDeviceId, destModule, mi.MethodName, mi.GetPayloadAsJson());
            var r = await client.InvokeDeviceMethodAsync(destDeviceId, destModule, mi);
            log.LogInformation("device method invocation complete");

            return;
        }
        public static async Task C2DOrientationMessage(string connectionString, string destDeviceId, string destModule, Orientation oState, string EventMsgTime, ILogger log)
        {
            ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString);
            log.LogInformation("have service client");
            OrientationMessage oMsg = new OrientationMessage();
            oMsg.OrientationState = oState;
            oMsg.OriginalEventUTCTime = EventMsgTime;
            var mi = new CloudToDeviceMethod(Keys.SetOrientation);
            mi.ConnectionTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.ResponseTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.SetPayloadJson(JsonConvert.SerializeObject(oMsg));

            // Invoke the direct method asynchronously and get the response from the simulated device.
            log.LogInformation("invoking device method for {0} {1} with {2} json {3}", destDeviceId, destModule, mi.MethodName, mi.GetPayloadAsJson());
            var r = await client.InvokeDeviceMethodAsync(destDeviceId, destModule, mi);
            log.LogInformation("device method invocation complete");

            return;
        }
        public static async Task C2DFruitMessage(string connectionString, string deviceId, string moduleId, string fruit, string EventMsgTime, ILogger log)
        {
            ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString);
            log.LogInformation("have service client");
            FruitMessage fruitMsg = new FruitMessage();
            fruitMsg.FruitSeen = fruit;
            fruitMsg.OriginalEventUTCTime = EventMsgTime;
            var mi = new CloudToDeviceMethod(Keys.SetFruit);
            mi.ConnectionTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.ResponseTimeout = TimeSpan.FromSeconds(DEFAULT_NETWORK_TIMEOUT);
            mi.SetPayloadJson(JsonConvert.SerializeObject(fruitMsg));

            // Invoke the direct method asynchronously and get the response from the simulated device.
            log.LogInformation("invoking device method for {0} {1} with {2} json {3}", deviceId, moduleId, mi.MethodName, mi.GetPayloadAsJson());
            var r = await client.InvokeDeviceMethodAsync(deviceId, moduleId, mi);
            log.LogInformation("device method invocation complete");

            return;
        }

        //[Singleton]
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
                string sourceDeviceId = (string)message.SystemProperties[Keys.DeviceIdMetadata];
                string sourceModuleId = (string)message.SystemProperties[Keys.ModuleIdMetadata];
                log.LogInformation($"Have device id {sourceDeviceId}");
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
                string conn = config[Keys.HubConnectionString];
                log.LogInformation("Have connection string {0}", conn);
                var rm = RegistryManager.CreateFromConnectionString(conn);
                log.LogInformation("Have registry manager");
                var destDevices = await GetDestinationDevices(sourceDeviceId, rm, log);
                log.LogInformation("Have topology");
                ModuleLoadedMessage loadMsg = JsonConvert.DeserializeObject<ModuleLoadedMessage>(msg);
                if ((loadMsg != null) && (loadMsg.ModuleName != null) && (loadMsg.ModuleName.Length > 0))
                {
                    if (sourceModuleId != loadMsg.ModuleName)
                    {
                        log.LogError("ignoring message because metadata inconsistency.  system property moduleId = {0} message module {1}", sourceModuleId, loadMsg.ModuleName);
                        return;
                    }
                    log.LogInformation("Load Module {0}", loadMsg.ModuleName);

                    var destModulesFromRouteList = LoadMsgRouting.Where(routeSourceModuleEntry => routeSourceModuleEntry.Item1 == sourceModuleId);
                    int destModulesFromRouteSize = destModulesFromRouteList.Count();
                    if (destModulesFromRouteSize == 0)
                    {
                        log.LogInformation("no destination routes for module {0} -- ignoring", sourceModuleId);
                        return;
                    }
                    if (destModulesFromRouteSize > 1)
                    {
                        log.LogError("configuration error. found {0} route destination lists. expected at most 1.", destModulesFromRouteSize);
                        return;
                    }
                    var destModulesFromRoute = destModulesFromRouteList.First().Item2;
                    foreach (var destDeviceId in destDevices)
                    {
                        log.LogInformation("examining destination device {0}", destDeviceId);
                        List<string> destCandidateNames = await GetDeviceModulesAsync(destDeviceId, rm, log);

                        var destModules = destCandidateNames.Where(n => n != sourceModuleId).Intersect(destModulesFromRoute, StringComparer.InvariantCulture);
                        log.LogInformation("sending loadmsg to destination {0} modules", destModules.Count());
                        foreach (var destModule in destModules)
                        {
                            try
                            {
                                log.LogInformation("sending loadmsg to destination module {0} on {1}", destModule, destDeviceId);
                                await C2DModuleMessage(conn, destDeviceId, destModule, sourceModuleId, log);
                            }
                            catch (Exception e)
                            {
                                log.LogInformation("{0} exception sending module load of {0} to device {1} module {2}", e.ToString(), sourceModuleId, destDeviceId, destModule);
                            }
                        }
                    }
                    return;
                }
                log.LogInformation("Not a Load Module -- checking for fruit");
                FruitMessage fruitMsg = JsonConvert.DeserializeObject<FruitMessage>(msg);
                if (fruitMsg.FruitSeen != null && fruitMsg.FruitSeen.Length > 0)
                {
                    log.LogInformation("fruit msg original time {0} fruit {1}", fruitMsg.OriginalEventUTCTime, fruitMsg.FruitSeen);
                    //log.LogInformation("found {0} slave devices", slaves.Length);
                    string originaleventtime = fruitMsg.OriginalEventUTCTime;
                    if (originaleventtime == null && message.Properties.ContainsKey(Keys.MessageCreationUTC))
                    {
                        originaleventtime = (string)message.Properties[Keys.MessageCreationUTC];
                    }
                    var destModulesFromRouteList = FruitMsgRouting.Where(routeSourceModuleEntry => routeSourceModuleEntry.Item1 == sourceModuleId);
                    int destModulesFromRouteSize = destModulesFromRouteList.Count();
                    if (destModulesFromRouteSize == 0)
                    {
                        log.LogInformation("no destination routes for module {0} -- ignoring", sourceModuleId);
                        return;
                    }
                    if (destModulesFromRouteSize > 1)
                    {
                        log.LogError("configuration error. found {0} route destination lists. expected at most 1.", destModulesFromRouteSize);
                        return;
                    }
                    var destModulesFromRoute = destModulesFromRouteList.First().Item2;
                    log.LogInformation("{0} destination modules for route", destModulesFromRoute.Count());
                    foreach (var destDeviceId in destDevices)
                    {
                        log.LogInformation("examining destination device {0}", destDeviceId);
                        List<string> destCandidateNames = await GetDeviceModulesAsync(destDeviceId, rm, log);
                        log.LogInformation("interecting {0} destmodules with {1} routemodules", destCandidateNames.Count(), destModulesFromRoute.Length);
                        var destModules = destCandidateNames.Intersect(destModulesFromRoute, StringComparer.InvariantCulture);
                        log.LogInformation("{0} modules after intersection", destModules.Count());
                        if (destModules == null || destModules.Count() == 0)
                        {
                            log.LogInformation("destination modules and routes list have empty intersection");
                        }
                        foreach (var destModule in destModules)
                        {
                            try
                            {
                                log.LogInformation("sending fruitmsg to destination module {0} on {1}", destModule, destDeviceId);
                                await C2DFruitMessage(conn, destDeviceId, destModule, fruitMsg.FruitSeen, originaleventtime, log);
                            }
                            catch (Exception e)
                            {
                                log.LogInformation("{0} exception sending module load of {0} to device {1} module {2}", e.ToString(), sourceModuleId, destDeviceId, destModule);
                            }
                        }
                    }
                    return;
                }
                log.LogInformation("Not a fruit message -- checking for orientation");
                OrientationMessage oMsg = JsonConvert.DeserializeObject<OrientationMessage>(msg);
                if (oMsg.OrientationState != Orientation.Unknown)
                {
                    log.LogInformation("orientation msg original time {0} orientation {1} sourceDeviceId {2} sourceModuleId {3}", oMsg.OriginalEventUTCTime, oMsg.OrientationState, sourceDeviceId, sourceModuleId);
                    //log.LogInformation("found {0} slave devices", slaves.Length);
                    string originaleventtime = oMsg.OriginalEventUTCTime;
                    if (originaleventtime == null && message.Properties.ContainsKey(Keys.MessageCreationUTC))
                    {
                        originaleventtime = (string)message.Properties[Keys.MessageCreationUTC];
                    }
                    var destModulesFromRouteList = OrientationMsgRouting.Where(routeSourceModuleEntry => routeSourceModuleEntry.Item1 == sourceModuleId);
                    int destModulesFromRouteSize = destModulesFromRouteList.Count();
                    if (destModulesFromRouteSize == 0)
                    {
                        log.LogInformation("no destination routes for module {0} -- ignoring", sourceModuleId);
                        return;
                    }
                    if (destModulesFromRouteSize > 1)
                    {
                        log.LogError("configuration error. found {0} route destination lists. expected at most 1.", destModulesFromRouteSize);
                        return;
                    }
                    log.LogInformation("orientation msg 1 route list as expected. examining {0} dest devices", destDevices.Count());
                    var destModulesFromRoute = destModulesFromRouteList.First().Item2;
                    foreach (var destDeviceId in destDevices)
                    {
                        log.LogInformation("examining destination device {0}", destDeviceId);
                        List<string> destCandidateNames = await GetDeviceModulesAsync(destDeviceId, rm, log);

                        var destModules = destCandidateNames.Where(n => n != sourceModuleId).Intersect(destModulesFromRoute, StringComparer.InvariantCulture);
                        log.LogInformation("sending loadmsg to destination {0} modules", destModules.Count());
                        foreach (var destModule in destModules)
                        {
                            try
                            {
                                log.LogInformation("sending orientation msg to destination module {0} on {1}", destModule, destDeviceId);
                                await C2DOrientationMessage(conn, destDeviceId, destModule, oMsg.OrientationState, originaleventtime, log);
                            }
                            catch (Exception e)
                            {
                                log.LogInformation("{0} exception sending module load of {0} to device {1} module {2}", e.ToString(), sourceModuleId, destDeviceId, destModule);
                            }
                        }
                    }
                    return;
                }

            }
            catch (Exception e)
            {
                log.LogInformation("{0} HubRun failed with exception {1}", Thread.CurrentThread.ManagedThreadId, e.ToString());
            }
            log.LogInformation("{0}HubRun complete", Thread.CurrentThread.ManagedThreadId);
            return;
        }
    }
}