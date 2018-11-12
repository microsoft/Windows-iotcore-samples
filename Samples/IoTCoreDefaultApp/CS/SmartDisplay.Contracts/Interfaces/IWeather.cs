// Copyright (c) Microsoft Corporation. All rights reserved.

using System.Threading.Tasks;

namespace SmartDisplay.Contracts
{
    public interface IWeather
    {
        string Name { get; }

        Task<GenericWeather> GetGenericWeatherAsync(double latitude, double longitude);
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
        public float TemperatureFahrenheit { get; set; }
        public float TemperatureCelsius { get; set; }
        public string AdditionalInfo { get; set; }
        public string WeatherDescription { get; set; }
    }

    public class GenericForecast
    {
        public GenericForecastDay[] Days { get; set; }
    }

    public class GenericForecastDay
    {
        public string DayOfWeek { get; set; }
        public string TemperatureFahrenheit { get; set; }
        public string TemperatureCelsius { get; set; }
        public string WeatherIcon { get; set; }
        public string WeatherDescription { get; set; }
    }
}
