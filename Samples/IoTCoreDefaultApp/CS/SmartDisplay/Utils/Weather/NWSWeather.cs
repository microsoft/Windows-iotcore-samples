// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDisplay.Weather
{
    /// <summary>
    /// Weather provided by the National Weather Service APIs
    /// https://forecast-v3.weather.gov/documentation?redirect=legacy
    /// </summary>
    public class NWSWeather : IWeather
    {
        public string Name { get; } = "National Weather Service";

        public string Type { get; set; }
        public NWSProperties Properties { get; set; }
        public bool UseWebIcon { get; set; } = false;

        private const string ForecastQueryFormat = "https://api.weather.gov/points/{0},{1}/forecast";
        public async Task<NWSWeather> GetForecastAsync(double latitude, double longitude)
        {
            NWSWeather weather = null;
            try
            {
                using (var cts = new CancellationTokenSource(10000))
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SmartDisplay");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    var uri = new Uri(string.Format(ForecastQueryFormat, latitude, longitude));

                    var response = await client.GetAsync(uri, cts.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        var data = await client.GetStringAsync(uri);
                        weather = JsonConvert.DeserializeObject<NWSWeather>(data);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.WriteException(ex);
            }

            return weather;
        }

        public async Task<GenericWeather> GetGenericWeatherAsync(double latitude, double longitude)
        {
            return (await GetForecastAsync(latitude, longitude))?.AsGenericWeather();
        }

        public GenericWeather AsGenericWeather()
        {
            var genericWeather = new GenericWeather
            {
                Source = Name,
                CurrentObservation = GetGenericCurrentObservation(),
                Forecast = GetGenericForecast()
            };

            return genericWeather;
        }

        /// <summary>
        /// Converts NWS data into GenericCurrentObservation object
        /// </summary>
        /// <returns></returns>
        public GenericCurrentObservation GetGenericCurrentObservation()
        {
            if (Properties == null || Properties.Periods == null || Properties.Periods.Length < 1)
            {
                return null;
            }

            var currentObservation = new GenericCurrentObservation();
            var currentPeriod = Properties.Periods.FirstOrDefault();

            currentObservation.Icon = (UseWebIcon) ? currentPeriod.Icon : ConvertIconToEmoji(currentPeriod.Icon);
            if (currentPeriod.TemperatureUnit == "F")
            {
                currentObservation.Temperature = currentPeriod.Temperature;
            }
            else
            {
                currentObservation.Temperature = WeatherHelper.GetFahrenheit(currentPeriod.Temperature);
            }
            currentObservation.WeatherDescription = currentPeriod.ShortForecast;
            currentObservation.AdditionalInfo = string.Format(Common.GetLocalizedText("WeatherWindSpeedFormat"), currentPeriod.WindDirection, currentPeriod.WindSpeed);

            return currentObservation;
        }

        /// <summary>
        /// Converts NWS data to GenericForcast object
        /// </summary>
        /// <returns></returns>
        public GenericForecast GetGenericForecast()
        {
            if (Properties == null || Properties.Periods == null || Properties.Periods.Length < 1)
            {
                return null;
            }

            var forecastDays = new List<GenericForecastDay>();
            var dateList = new List<DateTime>();
            foreach (var period in Properties.Periods)
            {
                if (!dateList.Contains(period.StartTime.Date))
                {
                    forecastDays.Add(new GenericForecastDay
                    {
                        Date = period.StartTime.Date,
                        TemperatureHigh = ((period.TemperatureUnit == "F") ? period.Temperature : WeatherHelper.GetFahrenheit(period.Temperature)),
                        TemperatureLow = ((period.TemperatureUnit == "F") ? period.Temperature : WeatherHelper.GetFahrenheit(period.Temperature)),
                        WeatherIcon = (UseWebIcon) ? period.Icon : ConvertIconToEmoji(period.Icon),
                        WeatherDescription = period.ShortForecast
                    });

                    dateList.Add(period.StartTime.Date);
                }
                else
                {
                    var existing = forecastDays.FirstOrDefault(x => x.Date == period.StartTime.Date);
                    if (existing != null)
                    {
                        existing.TemperatureLow = ((period.TemperatureUnit == "F") ? period.Temperature : WeatherHelper.GetFahrenheit(period.Temperature));
                    }

                    // Swap if needed
                    if (existing.TemperatureLow > existing.TemperatureHigh)
                    {
                        var temp = existing.TemperatureLow;
                        existing.TemperatureLow = existing.TemperatureHigh;
                        existing.TemperatureHigh = temp;
                    }
                }
            }

            return new GenericForecast
            {
                Days = forecastDays.Take(5).ToArray()
            };
        }

        /// <summary>
        /// Tries to convert NWS icon path to an emoji
        /// </summary>
        public static string ConvertIconToEmoji(string icon)
        {
            string iconPath = null;
            var match = Regex.Match(icon, @"https:\/\/api.weather.gov\/icons\/.*?\/.*?\/(.*?)\?");
            if (match.Success)
            {
                iconPath = match.Groups[1].Value;
            }

            switch (iconPath)
            {
                case "bkn":             // mostly cloudy
                case "novc":            // night overcast
                case "ovc":             // overcast
                case "nwind_sct":       // increasing clouds and blustery
                    return "☁️";
                case "skc":             // fair
                    return "☀️";
                case "nskc":            // night fair
                    return "🌙";
                case "fg":              // fog, mist
                case "nfg":             // night fog, mist
                case "nsmoke":          // night smoke
                case "smoke":           // smoke
                    return "🌫️";
                case "fzra":            // freezing rain
                case "fzrara":          // freezing rain rain
                case "hi_nshwrs":       // night showers in vicinity
                case "hi_shwrs":        // showers in vicinity
                case "mix":             // freezing rain snow
                case "nfzra":           // night freezing rain
                case "nfzrara":         // night freezing rain rain
                case "nmix":            // night freezing rain snow
                case "nra":             // night rain
                case "nrasn":           // night rain snow
                case "nra1":            // night light rain
                case "nshra":           // night rain showers
                case "ra":              // rain likely
                case "ra_sn":           // chance rain/snow
                case "shra":            // slight chance showers
                    return "🌧️";
                case "nraip":           // night rain ice pellets
                case "ip":              // ice pellets
                case "nip":             // night ice pellets
                    return "❄️";
                case "hi_ntsra":        // night thunderstorm in vicinity
                case "hi_tsra":         // thunderstorm in vicinity
                case "ntsra":           // night thunderstorm
                case "tsra":            // thunderstorm
                    return "🌩️";
                case "nra_sn":          // chance rain/snow
                case "nsn":             // night snow
                case "sn":              // snow
                    return "🌨️";
                case "nwind":           // night wind
                case "wind":            // wind
                case "wind_few":        // sunny and breezy
                case "wind_skc":        // sunny and breezy
                    return "🍃";
                case "few":             // a few clouds
                case "nbkn":            // night mostly cloudy 
                case "nfew":            // night a few clouds
                case "nsct":            // night partly cloudy
                case "sct":             // partly cloudy
                    return "⛅";
                default:
                    if (iconPath.Contains("rain"))
                    {
                        return "🌧️";
                    }
                    return "⛅";
            }
        }
    }

    public class NWSProperties
    {
        public DateTimeOffset Updated { get; set; }
        public string Units { get; set; }
        public string ForecastGenerator { get; set; }
        public DateTimeOffset GeneratedAt { get; set; }
        public DateTimeOffset UpdateTime { get; set; }
        public string ValidTimes { get; set; }
        public NWSElevation Elevation { get; set; }
        public NWSPeriod[] Periods { get; set; }
    }

    public class NWSElevation
    {
        public long Value { get; set; }
        public string UnitCode { get; set; }
    }

    public class NWSPeriod
    {
        public long Number { get; set; }
        public string Name { get; set; }
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public bool IsDaytime { get; set; }
        public int Temperature { get; set; }
        public string TemperatureUnit { get; set; }
        public string TemperatureTrend { get; set; }
        public string WindSpeed { get; set; }
        public string WindDirection { get; set; }
        public string Icon { get; set; }
        public string ShortForecast { get; set; }
        public string DetailedForecast { get; set; }
    }
}
