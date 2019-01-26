// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Weather;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDisplay.Utils
{
    public class WeatherProvider
    {
        public static WeatherProvider Instance { get; } = new WeatherProvider();

        public List<IWeather> Sources { get; } = new List<IWeather>
        {
            new NWSWeather(),
            new InternationalWeather(),
        };

        public ILogService LogService => ServiceUtil.LogService;

        private string _preferredSource = null;
        private const string DefaultSource = "DEFAULT";

        /// <summary>
        /// Tries to get weather information from each of its sources in order
        /// </summary>
        /// <param name="latitude"></param>
        /// <param name="longitude"></param>
        /// <returns></returns>
        public async Task<GenericWeather> GetGenericWeatherAsync(double latitude, double longitude)
        {
            // Read name of preferred weather provider from file if it exists (e.g. NWSWeather)
            if (_preferredSource == null)
            {
                _preferredSource = await FileUtil.ReadFileAsync("WeatherProvider.config") ?? DefaultSource;
            }

            // Try the preferred source first
            if (_preferredSource != DefaultSource)
            {
                LogService.Write($"Preferred source has been specified: {_preferredSource}");

                // Check if the preferred source matches an available source
                IWeather source = Sources.FirstOrDefault(x => x.GetType().Name == _preferredSource);
                if (source != null)
                {
                    try
                    {
                        LogService.Write($"Getting weather from: {source.Name}...");
                        var weather = await source.GetGenericWeatherAsync(latitude, longitude);
                        if (weather != null)
                        {
                            return weather;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteException(ex);
                    }
                }
            }

            // Query each weather provider until we get a response or exhaust the list
            foreach (var source in Sources)
            {
                try
                {
                    LogService.Write($"Getting weather from: {source.Name}...");
                    var weather = await source.GetGenericWeatherAsync(latitude, longitude);
                    if (weather != null)
                    {
                        return weather;
                    }
                }
                catch (Exception ex)
                {
                    LogService.WriteException(ex);
                }
            }

            return null;
        }
    }
}
