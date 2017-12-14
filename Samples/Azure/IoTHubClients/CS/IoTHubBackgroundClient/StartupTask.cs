using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;

using Windows.ApplicationModel.Background;

namespace IoTHubBackgroundClient
{
    public sealed class StartupTask : IBackgroundTask
    {
        static string DeviceConnectionString = "{your device connection string}";
        static DeviceClient Client = null;
        static TwinCollection reportedProperties = new TwinCollection();
        static CancellationTokenSource cts;
        static double baseTemperature = 60;
        static double basePressure = 500;
        static double baseHumidity = 50;

        const string SetTemperature = "setTemperature";
        const string SettingValue = "value";

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            _deferral = taskInstance.GetDeferral();

            Debug.WriteLine("Raspberry Pi IoT Central example");

            try
            {
                InitClient();

                cts = new CancellationTokenSource();
                SendTelemetryAsync(cts.Token);

                Debug.WriteLine("Wait for settings update...");
                Client.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null);

                SendDeviceProperties();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public static void InitClient()
        {
            try
            {
                Debug.WriteLine("Connecting to hub");
                Client = DeviceClient.CreateFromConnectionString(DeviceConnectionString, TransportType.Mqtt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        public async void SendDeviceProperties()
        {
            try
            {
                Debug.WriteLine("Sending device properties:");
                Random random = new Random();
                int newValue = random.Next(1, 6);

                // Show in debug window
                Debug.WriteLine("Die value: " + newValue);

                // Send to device twin
                TwinCollection telemetryConfig = new TwinCollection();
                reportedProperties["dieNumber"] = newValue;
                Debug.WriteLine(JsonConvert.SerializeObject(reportedProperties));

                await Client.UpdateReportedPropertiesAsync(reportedProperties);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private async void SendTelemetryAsync(CancellationToken token)
        {
            try
            {
                Random rand = new Random();

                while (true)
                {
                    double currentTemperature = baseTemperature + rand.NextDouble() * 20;
                    double currentPressure = basePressure + rand.NextDouble() * 100;
                    double currentHumidity = baseHumidity + rand.NextDouble() * 20;

                    // Show in debug window
                    Debug.WriteLine("Temperature: " + currentTemperature + ", Pressure: " + currentPressure + ", Humidity: " + currentHumidity);

                    var telemetryDataPoint = new
                    {
                        humidity = currentHumidity,
                        pressure = currentPressure,
                        temperature = currentTemperature
                    };
                    var messageString = JsonConvert.SerializeObject(telemetryDataPoint);
                    var message = new Message(Encoding.ASCII.GetBytes(messageString));

                    token.ThrowIfCancellationRequested();
                    await Client.SendEventAsync(message);

                    Debug.WriteLine("{0} > Sending telemetry: {1}", DateTime.Now, messageString);

                    await Task.Delay(1000);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Intentional shutdown: {0}", ex.Message);
            }
        }

        private async Task HandleSettingChanged(TwinCollection desiredProperties, object userContext)
        {
            try
            {
                Debug.WriteLine("Received settings change...");
                if (desiredProperties.Contains(SetTemperature))
                {
                    JValue settingValue = desiredProperties[SetTemperature][SettingValue];
                    if (settingValue.Type == JTokenType.Float || settingValue.Type == JTokenType.Integer)
                    {
                        // Show in the debug window
                        Debug.WriteLine("Temperature Setting: " + settingValue.Value);

                        // Act on setting change, then
                        AcknowledgeSettingChange(desiredProperties, SetTemperature);
                    }
                }

                await Client.UpdateReportedPropertiesAsync(reportedProperties);
            }

            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private static void AcknowledgeSettingChange(TwinCollection desiredProperties, string setting)
        {
            reportedProperties[setting] = new
            {
                value = desiredProperties[setting]["value"],
                status = "completed",
                desiredVersion = desiredProperties["$version"],
                message = "Processed"
            };
        }

        private void OnSendDeviceProperties(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            SendDeviceProperties();
        }

        private BackgroundTaskDeferral _deferral;
    }
}
