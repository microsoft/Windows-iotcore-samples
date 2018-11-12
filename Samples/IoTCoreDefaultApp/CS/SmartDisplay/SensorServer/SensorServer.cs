// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using SmartDisplay.Contracts;
using SmartDisplay.Weather;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace SmartDisplay.Sensors
{
    /// <summary>
    /// Reads data from temperature and humidity sensors
    /// </summary>
    public sealed class SensorServer
    {
        public bool IsSensorInitialized { get; private set; }

        private ITempHumiditySensor _tempHumiditySensor = null;

        private IAppService AppService => SmartDisplay.AppService.GetForCurrentContext();
        private IIoTHubService IoTHubService => AppService.GetRegisteredService<IIoTHubService>();

        // Constructor
        public SensorServer()
        {
            IsSensorInitialized = false;
        }

        // Initialize the I2C sensor and send information to the Azure IoT Hub
        public async Task InitializeAsync()
        {
            // Initialize the temp/humidity sensor
            try
            {
                _tempHumiditySensor = new SI7021_TempHumiditySensor();
                await _tempHumiditySensor.InitAsync();
                await _tempHumiditySensor.ResetAsync();
                IsSensorInitialized = true;
                
                await SendSensorDataToAzureAsync();
            }
            catch (Exception e)
            {
                App.LogService.WriteException(e);
            }
        }

        public async Task SendSensorDataToAzureAsync()
        {
            try
            {
                // If no hub is configured, AzureIoTHub.IotHubHelper.HostName would be set as an empty string.
                if (IoTHubService != null && IoTHubService.IsDeviceClientConnected)
                {
                    var data = await GetSensorDataAsync();

                    data.deviceName = IoTHubService.DeviceId ?? string.Empty;

                    string sensorDataJson = SensorJsonSerializer(data);

                    App.LogService.Write("Sending temp/humidity data to hub: " + sensorDataJson);
                    await IoTHubService.SendEventAsync(Encoding.ASCII.GetBytes(sensorDataJson));

                    App.LogService.Write("Sensor data uploaded to azure for " + data.deviceName);
                }
                else
                {
                    App.LogService.Write("Azure IoT Hub is not connected");
                }
            }
            catch (Exception e)
            {
                App.LogService.Write(e.Message, LoggingLevel.Error);
            }
        }

        private async Task<string> DisplayGetInfoAsync(string jsonParam)
        {
            App.LogService.Write(GetType().Name + ": DisplayGetInfoAsync");

            var response = new { response = "succeeded", reason = "" };
            try
            {
                return JsonConvert.SerializeObject(await GetSensorDataAsync());
            }
            catch (Exception e)
            {
                response = new { response = "rejected:", reason = e.Message };
                App.LogService.Write("", LoggingLevel.Error);
            }

            return JsonConvert.SerializeObject(response);
        }

        public async Task<SensorsData> GetSensorDataAsync()
        {
            SensorsData data = new SensorsData();

            try
            {
                if (IsSensorInitialized)
                {
                    // Read temperature and humidity data
                    var currentTempC = await _tempHumiditySensor.GetTemperatureAsync();
                    var currentTempF = WeatherHelper.GetFahrenheit(Convert.ToInt32(currentTempC));
                    var currentHumidity = await _tempHumiditySensor.GetHumidityAsync();

                    // Round value to nearest integer and format data as strings
                    data.tempC = Convert.ToInt32(currentTempC).ToString();
                    data.tempF = Convert.ToInt32(currentTempF).ToString();
                    data.humidity = Convert.ToInt32(currentHumidity).ToString();
                }
            }
            catch (Exception ex)
            {
                App.LogService.Write(ex.Message, LoggingLevel.Error);
            }

            return data;
        }

        // Converts the Humidity/Temperature sensor data into a JSON string
        private string SensorJsonSerializer(SensorsData data)
        {
            MemoryStream stream = new MemoryStream();
            DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(SensorsData));
            serializer.WriteObject(stream, data);

            stream.Position = 0;
            StreamReader sr = new StreamReader(stream);
            return sr.ReadToEnd();
        }
    }

    // JSON/Data contract for sending temperature/humidity data
    [DataContract]
    public class SensorsData
    {
        [DataMember]
        internal string deviceName;

        [DataMember]
        internal string tempF = "Err";

        [DataMember]
        internal string tempC = "Err";

        [DataMember]
        internal string humidity = "Err";
    }
}
