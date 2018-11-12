// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;

namespace SmartDisplay.Sensors
{
    interface ITempHumiditySensor
    {
        /// <summary>
        /// Initialize temperature sensor device
        /// </summary>
        Task InitAsync();

        /// <summary>
        /// Resets the sensor
        /// </summary>
        Task ResetAsync();

        /// <summary>
        /// Returns the current temperature in Celsius
        /// </summary>
        /// <returns>Temperature in Celsius</returns>
        Task<double> GetTemperatureAsync();

        /// <summary>
        /// Return current % humidity
        /// </summary>
        /// <returns>Humidity % 0 - 100</returns>
        Task<double> GetHumidityAsync();
    }
}
