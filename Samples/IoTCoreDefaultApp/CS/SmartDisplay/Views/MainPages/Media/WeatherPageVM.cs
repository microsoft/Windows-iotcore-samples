// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Weather;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.ViewModels
{
    public class WeatherPageVM : BaseViewModel
    {
        #region UI properties

        public IEnumerable<GenericForecastDay> ForecastCollection
        {
            get { return GetStoredProperty<IEnumerable<GenericForecastDay>>(); }
            set { SetStoredProperty(value); }
        }

        public string CurrentTemperature
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool IsFahrenheit
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsWeatherVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsErrorMessageVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string ErrorTitle
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string ErrorSubtitle
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string AttributionText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        #endregion

        private SettingsProvider Settings => AppService.Settings as SettingsProvider;
        private ILogService LogService => AppService.LogService;
        private string TempUnit => (Settings.IsFahrenheit) ? "°F" : "°C";

        private GenericWeather _currentWeather = null;

        private ThreadPoolTimer _autoRefreshTimer;

        public async void SetUpVM()
        {
            try
            {
                PageService?.ShowLoadingPanel();

                if (await RefreshWeatherAsync())
                {
                    IsWeatherVisible = true;
                    IsErrorMessageVisible = false;

                    _autoRefreshTimer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
                    {
                        await RefreshWeatherAsync();
                    }, TimeSpan.FromHours(6));

                    PopulateCommandBar();

                    LogService.Write("Refresh timer started.");
                }
                else
                {
                    IsWeatherVisible = false;
                    IsErrorMessageVisible = true;
                }
            }
            finally
            {
                PageService?.HideLoadingPanel();
            }
        }

        public void TearDownVM()
        {
            _autoRefreshTimer?.Cancel();
            _autoRefreshTimer = null;
        }

        public async Task<bool> RefreshWeatherAsync()
        {
            try
            {
                var location = await LocationUtil.GetLocationAsync(Settings);
                
                // Couldn't get user's location
                if (location.Name == Common.GetLocalizedText("WeatherMapPinUnknownLabel"))
                {
                    ErrorTitle = Common.GetLocalizedText("LocationErrorTitle");
                    ErrorSubtitle = Common.GetLocalizedText("LocationErrorSubtitle");
                    return false;
                }

                var weather = await WeatherProvider.Instance.GetGenericWeatherAsync(
                    location.Position.Latitude,
                    location.Position.Longitude
                    );
                                
                // Couldn't get weather
                if (weather == null)
                {
                    ErrorTitle = Common.GetLocalizedText("GenericErrorTitle");
                    ErrorSubtitle = Common.GetLocalizedText("WeatherErrorSubtitle");
                    return false;
                }

                DisplayWeather(weather);

                _currentWeather = weather;

                return true;
            }
            catch (Exception ex)
            {
                LogService.WriteException(ex);
                return false;
            }
        }

        private void DisplayWeather(GenericWeather weather)
        {
            CurrentTemperature = $"{((Settings.IsFahrenheit) ? Math.Round(weather.CurrentObservation.Temperature) : Math.Round(WeatherHelper.GetCelsius(weather.CurrentObservation.Temperature)))}{TempUnit}";

            if (!Settings.IsFahrenheit)
            {
                foreach(var day in weather.Forecast.Days)
                {
                    day.TemperatureHigh = Math.Round(WeatherHelper.GetCelsius(day.TemperatureHigh));
                    day.TemperatureLow = Math.Round(WeatherHelper.GetCelsius(day.TemperatureLow));
                }
            }

            ForecastCollection = weather.Forecast.Days;

            AttributionText = weather.Source;
        }

        private void PopulateCommandBar()
        {
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            
            // Add page settings button
            PageService?.AddCommandBarButton(PageUtil.CreatePageSettingCommandBarButton(
                PageService,
                new WeatherSettingsControl
                {
                    Width = Constants.DefaultSidePaneContentWidth,
                    Background = new SolidColorBrush(Colors.Transparent),
                },
                Common.GetLocalizedText("WeatherSettingHeader/Text")));
            PageService?.AddCommandBarButton(CommandBarButton.Separator);

            // Add refresh button
            PageService?.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Refresh),
                Label = Common.GetLocalizedText("RefreshButton"),
                Handler = async (s, e) =>
                {
                    await RefreshWeatherAsync();
                },
            });
        }
    }
}
