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
using Windows.Storage;
using Windows.UI.Core;
using Windows.UI.Xaml;

namespace IoTHubForegroundClient
{
    public sealed partial class MainPage : Page
    {
        static string ConnectionStringFileName = "connection.string.iothub";
        static DeviceClient Client = null;
        static TwinCollection reportedProperties = new TwinCollection();
        static CancellationTokenSource cts;
        static double baseTemperature = 60;
        static double basePressure = 500;
        static double baseHumidity = 50;

        const string SetTemperature = "setTemperature";
        const string SettingValue = "value";

        bool _running = false;

        public MainPage()
        {
            this.InitializeComponent();
            Start("");
        }

        private async Task<string> GetConnectionString()
        {
            // GetConnectionString() should read the connection string from the TPM.
            // For simplicity, we will be reading it from a text file dropped at the Documents folder.
            // Note that this is NOT a secure method and is only used for simplicity.
            //
            // An ANSI text file need to be placed at:
            //  \\<ip>\c$\Data\Users\DefaultAccount\Documents\
            //  The contents should be only the connection string.
            //
            StorageFolder storageFolder = KnownFolders.DocumentsLibrary;
            try
            {
                StorageFile connectionStringFile = await storageFolder.GetFileAsync(ConnectionStringFileName);
                return await FileIO.ReadTextAsync(connectionStringFile);
            }
            catch (System.IO.FileNotFoundException)
            {
                ConnectStringBox.Text = "<The file " + ConnectionStringFileName + " is missing from the documents folder. Copy/Paste the device connection string here>";
            }
            return "";
        }

        private async void Start(string connectionString)
        {
            Debug.WriteLine("Raspberry Pi IoT Central example");

            try
            {
                if (String.IsNullOrEmpty(connectionString))
                {
                    connectionString = await GetConnectionString();
                }
                if (String.IsNullOrEmpty(connectionString))
                {
                    return;
                }

                ConnectStringBox.Text = connectionString;

                InitClient(connectionString);

                cts = new CancellationTokenSource();
                SendTelemetryAsync(cts.Token);

                Debug.WriteLine("Wait for settings update...");
                await Client.SetDesiredPropertyUpdateCallbackAsync(HandleSettingChanged, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private static void InitClient(string connectionString)
        {
            try
            {
                Debug.WriteLine("Connecting to hub");
                Client = DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error in sample: {0}", ex.Message);
            }
        }

        private async void ShowDieNumber(int value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                DieNumberBlock.Text = value.ToString();
            });
        }

        private async void SendDeviceProperties()
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

        private async void ShowTelemetry(double temperature, double pressure, double humidity)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                TemperatureBlock.Text = temperature.ToString("F", CultureInfo.InvariantCulture);
                PressureBlock.Text = pressure.ToString("F", CultureInfo.InvariantCulture);
                HumidityBlock.Text = humidity.ToString("F", CultureInfo.InvariantCulture);
            });
        }

        private async void SendTelemetryAsync(CancellationToken token)
        {
            // If we are already sending telemetry, let's not spawn another one...
            if (_running)
            {
                return;
            }
            _running = true;

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
                        temp = currentTemperature
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

        private async void ShowSetting(string setting, double value)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
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
                    JObject setting = (JObject)desiredProperties[SetTemperature];
                    JValue settingValue = (JValue)setting[SettingValue];
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

        private void OnConnect(object sender, RoutedEventArgs e)
        {
            Start(ConnectStringBox.Text);
        }
    }
}
