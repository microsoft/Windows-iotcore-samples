// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IWeather
    {
        string Name { get; }

        Task<GenericWeather> GetGenericWeatherAsync(double latitude, double longitude);

        GenericWeather AsGenericWeather();
    }

    /// <summary>
    /// Generic weather template, used by weatherpage.xaml.cs
    /// </summary>
    public class GenericWeather
    {
        public string Source;
        public GenericCurrentObservation CurrentObservation { get; set; }
        public GenericForecast Forecast { get; set; }
    }

    public class GenericCurrentObservation
    {
        public string Icon { get; set; }
        public double Temperature { get; set; }
        public string AdditionalInfo { get; set; }
        public string WeatherDescription { get; set; }
    }

    public class GenericForecast
    {
        public GenericForecastDay[] Days { get; set; }
    }

    public class GenericForecastDay
    {
        public DateTime Date { get; set; }
        public double TemperatureHigh { get; set; } = -1;
        public double TemperatureLow { get; set; } = -1;
        public string WeatherIcon { get; set; }
        public string WeatherDescription { get; set; }
    }
}
