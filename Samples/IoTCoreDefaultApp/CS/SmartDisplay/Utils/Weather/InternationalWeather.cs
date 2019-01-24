// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace SmartDisplay.Weather
{
    /// <summary>
    /// Contains both forecast data and current data from 'weatherunlocked.com'
    /// </summary>
    public class InternationalWeather : IWeather
    {
        public string Name { get; } = "WeatherUnlocked.com";

        public InternationalCurrentWeather CurrentWeather { get; set; }
        public InternationalForecastWeather ForecastWeather { get; set; }
        private readonly string[] DateFormats =
        {
            "dd/MM/yyyy",
        };

        // Required to calculate cooldown
        private readonly TimeSpan _refreshCooldown = new TimeSpan(2, 0, 0);
        private DateTime _lastRefreshedTime;
        private GenericWeather _cachedWeather;

        public InternationalWeather()
        {
        }

        public InternationalWeather(InternationalCurrentWeather current, InternationalForecastWeather forecast)
        {
            CurrentWeather = current;
            ForecastWeather = forecast;
        }
        public GenericWeather AsGenericWeather()
        {
            GenericWeather weather = new GenericWeather()
            {
                CurrentObservation = new GenericCurrentObservation()
                {
                    Icon = GetIconFromInternationalWeather(CurrentWeather.Wx_code),
                    Temperature = CurrentWeather.Temp_f,
                    WeatherDescription = CurrentWeather.Wx_desc,
                    AdditionalInfo = string.Format(Common.GetLocalizedText("HumidityText"), CurrentWeather.Humid_pct.ToString())
                },
                Forecast = new GenericForecast()
            };

            List<GenericForecastDay> forecastDays = new List<GenericForecastDay>();
            // weatherunlocked.com gives a 7-day forecast, starting with yesterday's weather - we want the forecast starting tomorrow
            foreach (var day in ForecastWeather.Days)
            {
                if (DateTime.TryParseExact(day.Date, DateFormats, CultureInfo.CurrentUICulture, DateTimeStyles.None, out DateTime result) &&
                    result > DateTime.Now)
                {
                    forecastDays.Add(GetGenericForecastDay(day));
                }
            }
            weather.Forecast.Days = forecastDays.ToArray();
            return weather;
        }

        public GenericForecastDay GetGenericForecastDay(InternationalForecastWeatherDay jsonDay)
        {
            // JSON provides information for every 3 hours - we want the weather at noon to represent the daily forecast
            InternationalForecastWeatherTimeframe timeframe = jsonDay.Timeframes[jsonDay.Timeframes.Length / 2];
            DateTime date = Convert.ToDateTime(jsonDay.Date.Substring(0, 10), CultureInfo.CurrentUICulture);
            GenericForecastDay day = new GenericForecastDay
            {
                Date = date.Date,
                TemperatureHigh = jsonDay.Temp_max_f,
                TemperatureLow = jsonDay.Temp_min_f,
                WeatherIcon = GetIconFromInternationalWeather(timeframe.Wx_code),
                WeatherDescription = timeframe.Wx_desc
            };

            return day;
        }

        /// <summary>
        /// Returns an icon string corresponding to the code from weatherunlocked.com
        /// </summary>
        public static string GetIconFromInternationalWeather(int code)
        {
            switch (code)
            {
                case 0:             // Sunny skies/clear skies
                    return "☀️";
                case 1:             // Partly cloudy skies
                case 2:             // Cloudy skies
                case 3:             // Overcast skies
                    return "☁️";
                case 10:            // Mist
                case 45:            // Fog
                case 49:            // Freezing fog
                    return "🌁";
                case 21:            // Patchy rain possible
                case 24:            // Patchy freezing drizzle possible
                case 50:            // Patchy light drizzle
                case 51:            // Light drizzle
                case 56:            // Freezing drizzle
                case 57:            // Heavy freezing drizzle
                case 60:            // Patchy light rain
                case 61:            // Light Rain
                case 62:            // Moderate rain at times
                case 63:            // Moderate rain
                case 64:            // Heavy rain at times
                case 65:            // Heavy rain
                case 66:            // Light freezing rain
                case 67:            // Moderate or heavy freezing rain
                case 80:            // Light rain shower
                case 81:            // Moderate or heavy rain shower
                case 82:            // Torrential rain shower
                    return "🌧️";
                case 22:            // Patchy snow possible
                case 23:            // Patchy sleet possible
                case 38:            // Blowing snow
                case 39:            // Blizzard
                case 68:            // Light sleet
                case 69:            // Moderate or heavy sleet
                case 70:            // Patchy light snow
                case 71:            // Light snow
                case 72:            // Patchy moderate snow
                case 73:            // Moderate snow
                case 74:            // Patchy heavy snow
                case 75:            // Heavy snow
                case 79:            // Ice pellets
                case 83:            // Light sleet showers
                case 84:            // Moderate or heavy sleet showers
                case 85:            // Light snow showers
                case 86:            // Moderate or heavy snow showers
                case 87:            // Light showers of ice pellets
                case 88:            // Moderate or heavy showers of ice pellets
                case 93:            // Patchy light snow with thunder
                case 94:            // Moderate or heavy snow with thunder
                    return "🌨️";
                case 29:            // Thundery outbreaks possible
                case 91:            // Patchy light rain with thunder
                case 92:            // Moderate or heavy rain with thunder
                    return "🌩️";
                default:
                    return "⛅";
            }
        }
        /// <summary>
        /// This method is to retrieve localized weather descriptions
        /// Languages supported by weatherunlocked.com that are also supported languages in the app:
        /// English, French, Spanish, Italian, German, Russian.
        /// 
        /// WeatherUnlocked.com does not support the following languages:
        /// Korean, Japanese, Portuguese, Chinese. They will return English instead for their descriptions (which is the default)
        /// For more information, visit https://developer.weatherunlocked.com/documentation/localweather/resources#descriptions
        /// </summary>
        /// <returns>two letter code representing the language or "default" if it's not supported by weatherunlocked.com</returns>
        public static string GetLanguage()
        {
            switch (CultureInfo.CurrentUICulture.TwoLetterISOLanguageName)
            {
                case "fr":
                case "es":
                case "it":
                case "de":
                case "ru":
                    return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
                case "ko":
                case "jp":
                case "pt":
                case "zh":
                default:
                    return "default";
            }
        }

        /// <summary>
        /// Returns GenericWeather, populated by weatherunlocked.com
        /// </summary>
        public async Task<GenericWeather> GetGenericWeatherAsync(double lat, double lon)
        {
            // Get the Timestamp of when the next refresh will be allowed and calculate the difference between that and now
            DateTime timeToRefresh = _lastRefreshedTime.Add(_refreshCooldown);

            // Return the cached weather data if the the refresh is still cooling down
            if (_cachedWeather != null && DateTime.Now.CompareTo(timeToRefresh) < 0)
            {
                return _cachedWeather;
            }

            try
            {
                InternationalForecastWeather jsonForecastData;
                InternationalCurrentWeather jsonCurrentData;

                // If the user provided a WeatherToken.config file, then read the keys from that. Otherwise, read it from Keys.cs
                WeatherAPIInfo weatherInfo = await WeatherHelper.ReadWeatherAPIInfo();

                using (var cancel = new CancellationTokenSource(20000))
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "SmartDisplay");
                    string data = string.Empty;
                    Uri currentUri = new Uri("http://api.weatherunlocked.com/api/current/" +
                        Convert.ToString(lat, CultureInfo.InvariantCulture) + "," + Convert.ToString(lon, CultureInfo.InvariantCulture) +
                        "?lang=" + GetLanguage() +
                        "&app_id=" + weatherInfo.AppId +
                        "&app_key=" + weatherInfo.AppKey);
                    var response = await client.GetAsync(currentUri).AsTask(cancel.Token);
                    if (response.IsSuccessStatusCode)
                    {
                        data = await client.GetStringAsync(currentUri);

                        jsonCurrentData = Newtonsoft.Json.JsonConvert.DeserializeObject<InternationalCurrentWeather>(data);

                        if (data.Contains("error"))
                        {
                            throw new Exception("Location not found");
                        }
                    }
                    else
                    {
                        return null;
                    }

                    Uri forecastUri = new Uri("http://api.weatherunlocked.com/api/forecast/" +
                        Convert.ToString(lat, CultureInfo.InvariantCulture) + "," + Convert.ToString(lon, CultureInfo.InvariantCulture) +
                        "?lang=" + GetLanguage() +
                        "&app_id=" + weatherInfo.AppId +
                        "&app_key=" + weatherInfo.AppKey);

                    response = await client.GetAsync(forecastUri).AsTask(cancel.Token);
                    data = string.Empty;
                    if (response.IsSuccessStatusCode)
                    {
                        data = await client.GetStringAsync(forecastUri);

                        jsonForecastData = Newtonsoft.Json.JsonConvert.DeserializeObject<InternationalForecastWeather>(data);

                        if (data.Contains("error"))
                        {
                            throw new Exception("Location not found");
                        }
                    }
                    else
                    {
                        return null;
                    }
                }

                var result = new InternationalWeather(jsonCurrentData, jsonForecastData).AsGenericWeather();
                result.Source = Name;

                // Update cached weather object and refresh timestamp
                _cachedWeather = result;
                _lastRefreshedTime = DateTime.Now;

                return result;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                return null;
            }
        }
    }
    /// <summary>
    /// Contains format for json returned by weatherunlocked.com for 'forecast weather'
    /// </summary>
    public class InternationalForecastWeather
    {
        public InternationalForecastWeatherDay[] Days { get; set; }
    }

    /// <summary>
    /// Contains format for 'day' information in json returned by weatherunlocked.com
    /// </summary>
    public class InternationalForecastWeatherDay
    {
        public string Date { get; set; }
        public string Sunrise_time { get; set; }
        public string Sunset_time { get; set; }
        public string Moonrise_time { get; set; }
        public string Moonset_time { get; set; }
        public float Temp_max_c { get; set; }
        public float Temp_max_f { get; set; }
        public float Temp_min_c { get; set; }
        public float Temp_min_f { get; set; }
        public float Precip_total_mm { get; set; }
        public float Precip_total_in { get; set; }
        public float Rain_total_mm { get; set; }
        public float Rain_total_in { get; set; }
        public float Snow_total_mm { get; set; }
        public float Snow_total_in { get; set; }
        public float Prob_precip_pct { get; set; }
        public float Humid_max_pct { get; set; }
        public float Humid_min_pct { get; set; }
        public float Windspd_max_mph { get; set; }
        public float Windspd_max_kmh { get; set; }
        public float Windspd_max_kts { get; set; }
        public float Windspd_max_ms { get; set; }
        public float Windgst_max_mph { get; set; }
        public float Windgst_max_kmh { get; set; }
        public float Windgst_max_kts { get; set; }
        public float Windgst_max_ms { get; set; }
        public float Slp_max_in { get; set; }
        public float Slp_max_mb { get; set; }
        public float Slp_min_in { get; set; }
        public float Slp_min_mb { get; set; }
        public InternationalForecastWeatherTimeframe[] Timeframes { get; set; }
    }

    /// <summary>
    /// Contains format for 'timeframe' information in json returned by weatherunlocked.com
    /// </summary>
    public class InternationalForecastWeatherTimeframe
    {
        public string Date { get; set; }
        public int Time { get; set; }
        public string Utcdate { get; set; }
        public int Utctime { get; set; }
        public string Wx_desc { get; set; }
        public int Wx_code { get; set; }
        public string Wx_icon { get; set; }
        public float Temp_c { get; set; }
        public float Temp_f { get; set; }
        public float Feelslike_c { get; set; }
        public float Feelslike_f { get; set; }
        public float Winddir_deg { get; set; }
        public string Winddir_compass { get; set; }
        public float Windspd_mph { get; set; }
        public float Windspd_kmh { get; set; }
        public float Windspd_kts { get; set; }
        public float Windspd_ms { get; set; }
        public float Windgst_mph { get; set; }
        public float Windgst_kmh { get; set; }
        public float Windgst_kts { get; set; }
        public float Windgst_ms { get; set; }
        public float Cloud_low_pct { get; set; }
        public float Cloud_mid_pct { get; set; }
        public float Cloud_high_pct { get; set; }
        public float Cloudtotal_pct { get; set; }
        public float Precip_mm { get; set; }
        public float Precip_in { get; set; }
        public float Rain_mm { get; set; }
        public float Rain_in { get; set; }
        public float Snow_mm { get; set; }
        public float Snow_in { get; set; }
        public float Snow_accum_cm { get; set; }
        public float Snow_accum_in { get; set; }
        public string Prob_precip_pct { get; set; }
        public float Humid_pct { get; set; }
        public float Dewpoint_c { get; set; }
        public float Dewpoint_f { get; set; }
        public float Vis_km { get; set; }
        public float Vis_mi { get; set; }
        public float Slp_mb { get; set; }
        public float Slp_in { get; set; }
    }

    /// <summary>
    /// Contains format for json returned by weatherunlocked.com for 'current weather'
    /// </summary>
    public class InternationalCurrentWeather
    {
        public float Lat { get; set; }
        public float Lon { get; set; }
        public float Alt_m { get; set; }
        public float Alt_ft { get; set; }
        public string Wx_desc { get; set; }
        public int Wx_code { get; set; }
        public string Wx_icon { get; set; }
        public float Temp_c { get; set; }
        public float Temp_f { get; set; }
        public float Feelslike_c { get; set; }
        public float Feelslike_f { get; set; }
        public float Humid_pct { get; set; }
        public float Windspd_mph { get; set; }
        public float Windspd_kmh { get; set; }
        public float Windspd_kts { get; set; }
        public float Windspd_ms { get; set; }
        public float Winddir_deg { get; set; }
        public string Winddir_compass { get; set; }
        public float Cloudtotal_pct { get; set; }
        public float Vis_km { get; set; }
        public float Vis_mi { get; set; }
        public object Vis_desc { get; set; }
        public float Slp_mb { get; set; }
        public float Slp_in { get; set; }
        public float Dewpoint_c { get; set; }
        public float Dewpoint_f { get; set; }
    }
}
