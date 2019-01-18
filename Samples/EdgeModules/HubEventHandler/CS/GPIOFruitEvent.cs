

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
    public static class GPIOFruitEvent
    {
        private static HttpClient client = new HttpClient();

        public static async Task<string> GetCurrentFruit(string deviceId, RegistryManager rm, ILogger log)
        {
            string fruit = null;

            // await twin = GetModuleTwinAsync(deviceId, moduleId);
            log.LogInformation($"finding master for {deviceId}");
            Twin selfTwin = await rm.GetTwinAsync(deviceId);
            var selfProps = selfTwin.Properties.Desired;
            var masterId = (string) selfProps[Keys.FruitMaster];
            log.LogInformation($"master device id {masterId} module id {Keys.FruitModuleId}");
            Twin masterTwin = await rm.GetTwinAsync(masterId, Keys.FruitModuleId);
            log.LogInformation("have master module twin");
            var masterProps = masterTwin.Properties.Reported;
            if (masterProps.Contains(Keys.FruitSeen))
            {
                fruit = masterProps[Keys.FruitSeen];
                log.LogInformation($"Current Fruit {fruit}");
            } else
            {
                log.LogInformation("master has not seen fruit");
            }
            return fruit;
        }
        public static async Task C2DMessage(string connectionString, string deviceId, string moduleId, string fruit, ILogger log)
        {
            if (fruit == null)
            {
                return; // nothing seen yet
            }
            //var tm = await rm.GetTwinAsync(deviceId, moduleId);
            //ModuleClient mc = ModuleClient.CreateFromConnectionString()
            ServiceClient client = ServiceClient.CreateFromConnectionString(connectionString);
            log.LogInformation("have service client");
            FruitMessage fruitMsg = new FruitMessage();
            fruitMsg.FruitSeen = fruit;
            var mi = new CloudToDeviceMethod("SetFruit");
            mi.SetPayloadJson(JsonConvert.SerializeObject(fruitMsg));
            //mi.SetPayloadJson(JsonConvert.SerializeObject(fruit));
            //C2DMessage msg = new C2DMessage();
            //client.SendAsync(deviceId, moduleId, msg);

            //MethodRequest mr = new MethodRequest("SetFruit", Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body)), TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));

            // Invoke the direct method asynchronously and get the response from the simulated device.
            //var response = await mc.InvokeMethodAsync(deviceId, mr);
            var r = await client.InvokeDeviceMethodAsync(deviceId, moduleId, mi);
            log.LogInformation("device method invoked for {0} {1} with {2}", deviceId, moduleId, mi.ToString());

            return;
        }
        [FunctionName("ModuleLoadHub")]
        public static async Task HubRun([IoTHubTrigger("messages/events", Connection = "EndpointConnectionString")]EventData message, 
                                        ILogger log, WebJobsExecutionContext context)
        {
            string msg = Encoding.UTF8.GetString(message.Body);
            log.LogInformation($"C# IoT Hub trigger function processed a message: {msg}");
            ModuleLoadMessage loadMsg = JsonConvert.DeserializeObject<ModuleLoadMessage>(msg);
            if (loadMsg.ModuleName == Keys.GPIOModuleId)
            {
                log.LogInformation("Load Module GPIO");
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
                string conn = config[Keys.HubConnectionString];
                log.LogInformation("Have connection string {0}", conn);
                var rm = RegistryManager.CreateFromConnectionString(conn);
                log.LogInformation("Have registry manager");
                string deviceName = (string) message.SystemProperties["iothub-connection-device-id"];
                log.LogInformation($"Have device id {deviceName}");
                string fruit = await GetCurrentFruit(deviceName, rm, log);
                log.LogInformation($"Have fruit {fruit}");

                await C2DMessage(conn, deviceName, loadMsg.ModuleName, fruit, log);
            } else
            {
                log.LogInformation("Not a GPIO Load Module -- ignoring");
            }
            log.LogInformation("HubRun complete");
            return;
        }
        [FunctionName("ModuleLoadEdge")]
        public static async Task EdgeRun([EdgeHubTrigger("input")]D2CMessage message,
            [EdgeHub(OutputName = "localoutput")] IAsyncCollector<D2CMessage> localoutput,
            [EdgeHub(OutputName = "upstream")] IAsyncCollector<D2CMessage> upstream,
            WebJobsExecutionContext context,
            ILogger log)
        {
            string msg = Encoding.UTF8.GetString(message.GetBytes());
            log.LogInformation($"C# IoT Edge Hub trigger function processed a message: {msg}");
            ModuleLoadMessage loadMsg = JsonConvert.DeserializeObject<ModuleLoadMessage>(msg);
            await upstream.AddAsync(message);  // send everything upstream
            if (loadMsg.ModuleName == Keys.GPIOModuleId)
            {
                //string conn = ConfigurationManager.AppSettings[Keys.HubConnectionString];
                log.LogInformation("Load Module GPIO");
                var config = new ConfigurationBuilder().SetBasePath(context.FunctionAppDirectory).AddJsonFile("local.settings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();
                string conn = config[Keys.HubConnectionString];
                log.LogInformation("Have connection string {0}", conn);
                var rm = RegistryManager.CreateFromConnectionString(conn);
                string deviceName = (string)message.ConnectionDeviceId;
                string fruit = await GetCurrentFruit(deviceName, rm, log);

                await C2DMessage(conn, deviceName, loadMsg.ModuleName, fruit, log);
            } else
            {
                await localoutput.AddAsync(message); // if it's not a gpio module load reflect it locally
            }
            return;
        }
    }
}