// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SmartDisplay.Weather
{
    /// <summary>
    /// Contains data from 'forecast.weather.gov'
    /// </summary>
    public class USWeather : IWeather
    {
        public string OperationalMode { get; set; }
        public string SrsName { get; set; }
        public DateTime CreationDate { get; set; }
        public string CreationDateLocal { get; set; }
        public string ProductionCenter { get; set; }
        public string Credit { get; set; }
        public string MoreInformation { get; set; }
        public USWeatherLocation Location { get; set; }
        public USWeatherTime Time { get; set; }
        public USWeatherData Data { get; set; }
        public USWeatherCurrentObservation CurrentObservation { get; set; }

        public string Name { get; } = "National Weather Service (forecast.weather.gov)";

        public GenericWeather AsGenericWeather()
        {
            var weather = new GenericWeather
            {
                CurrentObservation = new GenericCurrentObservation(),
                Forecast = new GenericForecast()
            };

            var forecastObjects = ParseForecastDayObjects();
            var currentForecast = forecastObjects.FirstOrDefault(x => x.StartValidTime.Date == DateTime.Now.Date && DateTime.Now >= x.StartValidTime);

            if (currentForecast != null)
            {
                weather.CurrentObservation.Icon = GetIconFromUSWeather(currentForecast.IconLink);
                weather.CurrentObservation.TemperatureFahrenheit = (float)currentForecast.Temperature;
                weather.CurrentObservation.TemperatureCelsius = WeatherHelper.GetCelsius((int)weather.CurrentObservation.TemperatureFahrenheit);
                weather.CurrentObservation.WeatherDescription = currentForecast.Weather;
            }

            // forecast.weather.gov gives 15 weather reports, including 2 for each day (one during the day, and one at night),
            // possibly starting with either "This Afternoon" or "Tonight" depending on the time of day.
            // Only grab the first forecast for each date, up to the number of days we specify
            var forecastDays = new List<GenericForecastDay>();
            int numDays = 5;

            foreach (var forecastObject in forecastObjects)
            {
                if (forecastDays.Count == numDays)
                {
                    break;
                }

                var date = forecastObject.StartValidTime.Date;
                // If we haven't already gotten a forecast for the day and it isn't today, add it to the list
                if (forecastDays.FirstOrDefault(x => x.DayOfWeek == date.ToString("dddd")) == null && date != DateTime.Now.Date)
                {
                    forecastDays.Add(GetGenericForecastDay(
                        date.ToString("dddd"),
                        ((int)forecastObject.Temperature).ToString(),
                        forecastObject.IconLink,
                        forecastObject.Weather));
                }
            }

            weather.Forecast.Days = forecastDays.ToArray();

            return weather;
        }

        public GenericForecastDay GetGenericForecastDay(string dayName, string tempF, string icon, string desc)
        {
            GenericForecastDay day = new GenericForecastDay
            {
                DayOfWeek = dayName,
                TemperatureFahrenheit = tempF + "°F",
                WeatherIcon = GetIconFromUSWeather(icon),
                WeatherDescription = desc
            };
            if (int.TryParse(tempF, out var temp))
            {
                day.TemperatureCelsius = WeatherHelper.GetCelsius(temp).ToString() + "°C";
            }

            return day;
        }

        /// <summary>
        /// Returns an icon string corresponding to the code from forecast.weather.gov
        /// </summary>
        public static string GetIconFromUSWeather(string icon)
        {
            string iconPath = icon;
            string[] temp;
            // If it's a DualImage type, only use the first image
            if (icon.IndexOf("DualImage.php?i=") > -1)
            {
                temp = icon.Split('=');
                iconPath = temp[1];
                temp = iconPath.Split('&');
                iconPath = temp[0];
            }
            temp = iconPath.Split('/');
            iconPath = temp[temp.Length - 1];
            temp = iconPath.Split('.');
            iconPath = temp[0];
            iconPath = System.Text.RegularExpressions.Regex.Replace(iconPath, @"[\d-]", string.Empty);

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

        /// <summary>
        /// Returns GenericWeather, populated by forecast.weather.gov
        /// </summary>
        public async Task<GenericWeather> GetGenericWeatherAsync(double lat, double lon)
        {
            try
            {
                USWeather usWeather;
                using (var cancel = new CancellationTokenSource(20000))
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SmartDisplay");
                    Uri uri = new Uri("http://forecast.weather.gov/MapClick.php?lat=" + lat + "&lon=" + lon + "&FcstType=json");
                    string data = string.Empty;
                    var response = await client.GetAsync(uri).AsTask(cancel.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        data = await client.GetStringAsync(uri);

                        if (data.Contains("{ \"success\": false") || data.Contains("javascript"))
                        {
                            return null;
                        }

                        usWeather = Newtonsoft.Json.JsonConvert.DeserializeObject<USWeather>(data);
                    }
                    else
                    {
                        return null;
                    }

                    var result = usWeather.AsGenericWeather();
                    result.Source = Name;

                    return result;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }

        /// <summary>
        /// Combine the forecast data from the separate arrays into a single array of objects
        /// </summary>
        /// <returns></returns>
        public IEnumerable<USWeatherForecastDayObject> ParseForecastDayObjects()
        {
            var objectList = new List<USWeatherForecastDayObject>();
            for (int i = 0; i < Time.StartValidTime.Length; i++)
            {
                try
                {
                    objectList.Add(new USWeatherForecastDayObject
                    {
                        StartPeriodName = Time.StartPeriodName[i],
                        StartValidTime = Time.StartValidTime[i],
                        IconLink = Data.IconLink[i],
                        Pop = Data.Pop[i],
                        Temperature = double.Parse(Data.Temperature[i]),
                        TempLabel = Time.TempLabel[i],
                        Text = Data.Text[i],
                        Weather = Data.Weather[i]
                    });
                }
                catch (Exception)
                {
                    // Catch out of bounds exceptions and other
                    // unexpected exceptions
                }
            }

            return objectList;
        }
    }

    /// <summary>
    /// Contains format for 'location' information in json returned by forecast.weather.gov
    /// </summary>
    public class USWeatherLocation
    {
        public string Region { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Elevation { get; set; }
        public string Wfo { get; set; }
        public string Timezone { get; set; }
        public string AreaDescription { get; set; }
        public string Radar { get; set; }
        public string Zone { get; set; }
        public string County { get; set; }
        public string Firezone { get; set; }
        public string Metar { get; set; }
    }

    /// <summary>
    /// Contains format for 'time' information in json returned by forecast.weather.gov
    /// </summary>
    public class USWeatherTime
    {
        public string LayoutKey { get; set; }
        public string[] StartPeriodName { get; set; }
        public DateTime[] StartValidTime { get; set; }
        public string[] TempLabel { get; set; }
    }

    /// <summary>
    /// Contains format for 'data' information in json returned by forecast.weather.gov
    /// </summary>
    public class USWeatherData
    {
        public string[] Temperature { get; set; }
        public string[] Pop { get; set; }
        public string[] Weather { get; set; }
        public string[] IconLink { get; set; }
        public string[] Hazard { get; set; }
        public string[] HazardUrl { get; set; }
        public string[] Text { get; set; }
    }

    /// <summary>
    /// Contains format for 'currentobservation' information in json returned by forecast.weather.gov
    /// </summary>
    public class USWeatherCurrentObservation
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Elev { get; set; }
        public string Latitude { get; set; }
        public string Longitude { get; set; }
        public string Date { get; set; }
        public string Temp { get; set; }
        public string Dewp { get; set; }
        public string Relh { get; set; }
        public string Winds { get; set; }
        public string Windd { get; set; }
        public string Gust { get; set; }
        public string Weather { get; set; }
        public string Weatherimage { get; set; }
        public string Visibility { get; set; }
        public string Altimeter { get; set; }
        public string SLP { get; set; }
        public string Timezone { get; set; }
        public string State { get; set; }
        public string WindChill { get; set; }
    }

    public class USWeatherForecastDayObject
    {
        public DateTime StartValidTime;
        public string StartPeriodName;
        public string TempLabel;
        public string Pop;
        public double Temperature;
        public string Text;
        public string IconLink;
        public string Weather;
    }
}
