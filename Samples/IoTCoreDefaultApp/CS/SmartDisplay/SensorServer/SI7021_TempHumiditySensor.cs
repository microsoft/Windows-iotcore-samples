// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.I2c;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace SmartDisplay.Sensors
{
    class SI7021_TempHumiditySensor : ITempHumiditySensor
    {
        private const string TempSensorI2CBusRpi = "I2C1";              // Friendly name for the I2C bus the sensor is installed on
        private const string TempSensorI2CBusHummingBoard = "I2C3";     // I2C bus for HummingBoard
        private const int SlaveAddress = 0x40;       // there is only one hard coded slave address for the SI7021 sensor

        private bool _isInitialized = false;

        // SI7021 registers
        private const byte TriggerTemperatureMeasurement = 0xE3;
        private const byte TriggerHumidityMeasurement = 0xE5;
        private const byte TriggerReset = 0xFE;

        // I2C temp/humidity sensor
        private I2cDevice _i2cTempHumiditySensor = null;

        /// <summary>
        /// Initialize I2C device
        /// </summary>
        public async Task InitAsync()
        {
            var tempHumidityBus = string.Empty;
            var deviceInfo = new EasClientDeviceInformation();
            if (deviceInfo.SystemProductName.Contains("HummingBoard"))
            {
                tempHumidityBus = TempSensorI2CBusHummingBoard;
            }
            else if (deviceInfo.SystemProductName.Contains("Raspberry"))
            {
                tempHumidityBus = TempSensorI2CBusRpi;
            }
            else
            {
                throw new Exception("Unsupported device: " + deviceInfo.SystemProductName);
            }

            // Find the I2C bus controller
            string aqs = I2cDevice.GetDeviceSelector(tempHumidityBus);
            var _i2cDeviceInfo = await DeviceInformation.FindAllAsync(aqs);
            if (_i2cDeviceInfo.Count != 1)
            {
                throw new Exception("Failed to get I2C bus \"" + tempHumidityBus + "\"");
            }

            // Create the I2C device
            I2cConnectionSettings i2cTempHumiditySettings = new I2cConnectionSettings(SlaveAddress);
            _i2cTempHumiditySensor = await I2cDevice.FromIdAsync(_i2cDeviceInfo[0].Id, i2cTempHumiditySettings);

            if (_i2cTempHumiditySensor == null)
            {
                throw new Exception("Failed to initialize SI7021 temp/humidity sensor on I2C bus.");
            }

            _isInitialized = true;
        }

        /// <summary>
        /// Resets the sensor
        /// </summary>
        public async Task ResetAsync()
        {
            if (!_isInitialized)
            {
                throw new Exception("Sensor is not initialized");
            }

            byte[] resetCommand = new byte[1] { TriggerReset };
            _i2cTempHumiditySensor.Write(resetCommand);

            // Need to wait at least 80 ms before talking to sensor through I2C interface
            await Task.Delay(100);
        }

        /// <summary>
        /// Returns the current temperature in Celsius
        /// </summary>
        /// <returns>Temperature in Celsius</returns>
        public async Task<double> GetTemperatureAsync()
        {
            if (!_isInitialized)
            {
                throw new Exception("Sensor is not initialized");
            }

            byte[] tempCommand = new byte[1] { TriggerTemperatureMeasurement };
            byte[] tempData = new byte[2];

            _i2cTempHumiditySensor.Write(tempCommand);

            // Per datasheet 14-bit temperature needs 10.8 msec
            await Task.Delay(50);

            _i2cTempHumiditySensor.Read(tempData);

            // Combine bytes
            var rawTempReading = tempData[0] << 8 | tempData[1];
            // Calculate relative temperature signal output
            var tempRatio = rawTempReading / (float)65536;
            // Temp conversion formula per SI7021 datasheet
            double temperature = (-46.85 + (175.72 * tempRatio));
            return temperature;
        }

        /// <summary>
        /// Return current % humidity
        /// </summary>
        /// <returns>Humidity % 0 - 100</returns>
        public async Task<double> GetHumidityAsync()
        {
            if (!_isInitialized)
            {
                throw new Exception("Sensor is not initialized");
            }

            byte[] humidityCommand = new byte[1] { TriggerHumidityMeasurement };
            byte[] humidityData = new byte[2];

            _i2cTempHumiditySensor.Write(humidityCommand);

            // Per datasheet 12-bit humidity needs 12 msec, in practice it takes much longer
            await Task.Delay(50);

            _i2cTempHumiditySensor.Read(humidityData);

            // Combine bytes
            var rawHumidityReading = humidityData[0] << 8 | humidityData[1];
            // Calculate relative humidity signal output
            var humidityRatio = rawHumidityReading / (float)65536;
            // Humidity conversion formulate per SI7021 datasheet
            double humidity = -6 + (125 * humidityRatio);
            return humidity;
        }
    }
}
