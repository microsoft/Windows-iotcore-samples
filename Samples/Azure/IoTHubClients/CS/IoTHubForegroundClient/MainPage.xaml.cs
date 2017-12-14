using Newtonsoft.Json.Linq;

using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;

namespace IoTHubForegroundClient
{
    public sealed partial class MainPage : Page
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

        public MainPage()
        {
            this.InitializeComponent();
            Debug.WriteLine("Raspberry Pi IoT Central example");

            try
            {
                InitClient();

                cts = new CancellationTokenSource();
                SendTelemetryAsync(cts.Token);

                Debug.WriteLine("Wait for settings update...");
                Client.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null);
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

        private void ShowDieNumber(int value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DieNumberBlock.Text = value.ToString();
            });
        }

        public async void SendDeviceProperties()
        {
            try
            {
                Debug.WriteLine("Sending device properties:");
                Random random = new Random();
                int newValue = random.Next(1, 6);

                // Show in UI
                ShowDieNumber(newValue);

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

        private void ShowTelemetry(double temperature, double pressure, double humidity)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TemperatureBlock.Text = temperature.ToString("F", CultureInfo.InvariantCulture);
                PressureBlock.Text = pressure.ToString("F", CultureInfo.InvariantCulture);
                HumidityBlock.Text = humidity.ToString("F", CultureInfo.InvariantCulture);
            });
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

                    ShowTelemetry(currentTemperature, currentPressure, currentHumidity);

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

        private void ShowSetting(string setting, double value)
        {
            CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                switch (setting)
                {
                    case SetTemperature:
                        SetTemperatureBlock.Text = value.ToString("F", CultureInfo.InvariantCulture);
                        break;
                }
            });
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
                        ShowSetting(SetTemperature, Double.Parse(settingValue.Value.ToString()));
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
    }
}