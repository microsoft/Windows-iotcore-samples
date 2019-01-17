// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using SmartDisplay.Utils;
using System;
using System.Threading.Tasks;
using Windows.Services.Maps;

namespace SmartDisplay.Weather
{
    /// <summary>
    /// Contains helper methods used by WeatherPage
    /// </summary>
    public static class WeatherHelper
    {
        /// <summary>
        /// Returns degrees in celsius, given degrees in fahrenheit
        /// </summary>
        public static double GetCelsius(double fahrenheit)
        {
            return ((fahrenheit - 32) * 5) / 9;
        }

        /// <summary>
        /// Returns degrees in fahrenheit, given degrees in celsius
        /// </summary>
        public static double GetFahrenheit(double celsius)
        {
            return ((celsius * 9) / 5) + 32;
        }

        /// <summary>
        /// Attempts to set the MapService token - this seems to be the only way
        /// to validate the token
        /// </summary>
        /// <param name="token"></param>
        /// <returns>True if successful, false if invalid token</returns>
        public static bool TrySetMapServiceToken(string token)
        {
            // If the token is invalid, it'll actually set the token to something
            // invalid and then throw the exception, so we need to save the old value
            string oldToken = MapService.ServiceToken;
            try
            {
                MapService.ServiceToken = token;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            // Try to reset the token - if it was in a bad state to begin with
            // it'll just throw another exception, so we need to catch that as well
            try
            {
                MapService.ServiceToken = oldToken;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return false;
        }

        /// <summary>
        /// Looks for the Map Token inside of <paramref name="fileName"/> in LocalState folder
        /// <para> e.g. C:\Data\USERS\[User Account]\AppData\Packages\[Package Full Name]\LocalState\MapToken.config</para>
        /// </summary>
        /// <param name="fileName"></param>
        public static async Task SetMapTokenFromFileAsync(string fileName)
        {
            ServiceUtil.LogService.Write($"Reading map token from {fileName}...");

            string mapToken = await FileUtil.ReadFileAsync(fileName);

            // Try to set the map token
            ServiceUtil.LogService.Write(
                (!string.IsNullOrWhiteSpace(mapToken) && TrySetMapServiceToken(mapToken)) ?
                "Map token has been successfully set" :
                "No map token found."
                );
        }

        /// <summary>
        /// Retrieve Weather API ID and Key. 
        /// This method checks 2 places: In SmartDisplay.Utils\Keys.cs and in WeatherToken.config file in LocalState (if it exists)
        /// </summary>
        public async static Task<WeatherAPIInfo> ReadWeatherAPIInfo()
        {
            // Check to see if the user provided a config file
            bool weatherConfigExists = await FileUtil.FileExistsAsync(Keys.WEATHER_CONFIG_FILENAME);

            // If the Default values in Keys.cs are present and there is no config file, there are no Weather API keys defined.
            if (!weatherConfigExists &&
                (Keys.WEATHER_APP_ID.Equals("<YOUR WEATHER API ID HERE>") || Keys.WEATHER_APP_KEY.Equals("<YOUR WEATHER API KEY HERE>")))
            {
                throw new Exception("WeatherAPIKeysNotDefined");
            }

            // If the config file exists, use the keys provided there
            if (weatherConfigExists)
            {
                try
                {
                    return JsonConvert.DeserializeObject<WeatherAPIInfo>(await FileUtil.ReadFileAsync(Keys.WEATHER_CONFIG_FILENAME));
                }
                catch (Exception ex)
                {
                    ServiceUtil.LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                }

                return null;
            }

            // Else they must be defined in Keys.cs
            return new WeatherAPIInfo(Keys.WEATHER_APP_ID, Keys.WEATHER_APP_KEY);
        }
    }

    /// <summary>
    /// Holds the weather App key and ID
    /// </summary>// 
    public class WeatherAPIInfo
    {
        public string AppId { get; set; }
        public string AppKey { get; set; }

        public WeatherAPIInfo(string appId, string appKey)
        {
            AppId = appId;
            AppKey = appKey;
        }
    }

    public class ForecastDay
    {
        public string Day;
        public string TempF;
        public string TempC;
        public string Icon;
        public string Desc;

        public ForecastDay()
        {
            Day = string.Empty;
            TempC = string.Empty;
            TempF = string.Empty;
            Icon = string.Empty;
            Desc = string.Empty;
        }
    }
}