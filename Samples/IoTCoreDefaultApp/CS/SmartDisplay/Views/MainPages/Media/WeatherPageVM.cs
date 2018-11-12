// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Sensors;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using SmartDisplay.Weather;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.ViewModels
{
    public class WeatherPageVM : BaseViewModel
    {
        #region UI properties

        #region Page Layout
        public GridLength LeftColumnWidth
        {
            get { return GetStoredProperty<GridLength>(); }
            set { SetStoredProperty(value); }
        }
        public GridLength RightColumnWidth
        {
            get { return GetStoredProperty<GridLength>(); }
            set { SetStoredProperty(value); }
        }
        public int TodaysWeatherColumnProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }
        public int WeatherColumnSpanProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }
        public int SensorRowProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }
        public int SensorColumnSpanProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }
        public int MapColumnProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }
        public int WeatherColumnProperty
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Map
        public ObservableCollection<MapLayer> MapLayers { get; } = new ObservableCollection<MapLayer>();
        public Geopoint MapCenter
        {
            get { return GetStoredProperty<Geopoint>(); }
            set { SetStoredProperty(value); }
        }
        public string CurrentLocation
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public MapColorScheme ColorScheme
        {
            get { return GetStoredProperty<MapColorScheme>(); }
            set { SetStoredProperty(value); }
        }
        #endregion

        #region CurrentLocation
        public string CurrentTime
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public string CurrentDate
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public string CurrentWeatherIcon
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public string CurrentTemperature
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public string CurrentAdditionalInfo
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public string CurrentWeather
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public bool SensorEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }
        public string SensorTemperatureAndHumidity
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        #endregion

        #region Forecast
        public ObservableCollection<ForecastDay> Forecast
        {
            get { return GetStoredProperty<ObservableCollection<ForecastDay>>(); }
            set { SetStoredProperty(value); }
        }
        public string WeatherProviderString
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public bool WeatherControlVisibility
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool WeatherErrorPageVisibility
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Units
        public bool CelsiusEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool FahrenheitEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }
        #endregion

        #endregion

        private SensorServer _sensorServer;

        private ThreadPoolTimer _clockTimer = null;
        private ThreadPoolTimer _updateWeatherTimer = null;
        private DateTime _latestMessageTimestamp;
        private const int MessageTimeLimit = 60; // Send a reading every hour to the Azure IoT Hub and flip the display
        private string WeatherLocation;

        private const double ExpandedMapWidth = 2.75;
        private const double NormalMapWidth = 1;
        private const double NarrowForecastWidth = 1;
        private const double NormalForecastWidth = 1.3;

        private bool _isFlipped = false;
        private double _leftWidth = NormalMapWidth;
        private double _rightWidth = NormalForecastWidth;

        private GenericWeather _weather;
        private SimpleLocation _currentLocation;

        private SettingsProvider Settings => AppService?.Settings as SettingsProvider;
        private ILogService LogService => AppService?.LogService;
        
        public WeatherPageVM() : base()
        {
            Forecast = new ObservableCollection<ForecastDay>();
            LeftColumnWidth = new GridLength(_leftWidth, GridUnitType.Star);
            RightColumnWidth = new GridLength(_rightWidth, GridUnitType.Star);
            FahrenheitEnabled = true;
            WeatherColumnSpanProperty = 1;
            SensorColumnSpanProperty = 1;
            MapColumnProperty = 0;
            WeatherLocation = null;
            WeatherControlVisibility = false;
            WeatherErrorPageVisibility = false;
            WeatherColumnProperty = 1;

            _latestMessageTimestamp = DateTime.MinValue;
            _sensorServer = new SensorServer();
            InitializeSensorServer();
        }

        public async void SetUpVM()
        {
            if (!AppService.IsConnectedInternet())
            {
                AppService.DisplayNoInternetDialog(typeof(DeviceInfoPage));
                return;
            }

            // Prompt user for location consent
            if (!Settings.IsLocationEnabled)
            {
                string locationPermissionPrompt = string.Format(Common.GetLocalizedText("LocationPermissionPromptText"), Common.GetLocalizedText("WeatherPageTileText"));
                Settings.IsLocationEnabled = await AppService.YesNoAsync(locationPermissionPrompt, Common.GetLocalizedText("LocationPermissionPromptDescription"));
            }

            Settings.SettingsUpdated += Settings_SettingsUpdated;

            PopulateCommandBar();

            WeatherLocation = Settings.WeatherLocationString;
            if (_clockTimer == null)
            {
                _clockTimer = ThreadPoolTimer.CreatePeriodicTimer(UpdateTimeAndDate, TimeSpan.FromMilliseconds(1000));
            }

            try
            {
                ShowLoadingPanel();
                await RefreshUiAsync();
            }
            finally
            {
                HideLoadingPanel();
            }
        }

        public void TearDownVM()
        {
            Settings.SettingsUpdated -= Settings_SettingsUpdated;

            // Hide the loading panel when leaving the page.
            HideLoadingPanel();

            _clockTimer?.Cancel();
            _clockTimer = null;

            _updateWeatherTimer?.Cancel();
            _updateWeatherTimer = null;
        }

        private ThreadPoolTimer _updateSettingsTimer;
        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            switch (args.Key)
            {
                case "WeatherLocationLatitude":
                case "WeatherLocationLongitude":
                case "MapSizeExpanded":
                case "MapFlipEnabled":
                    _updateSettingsTimer?.Cancel();
                    _updateSettingsTimer = ThreadPoolTimer.CreateTimer((t) =>
                    {
                        InvokeOnUIThread(async () => await RefreshUiAsync());
                    }, TimeSpan.FromSeconds(1));
                    break;
            }
        }

        private async Task RefreshUiAsync()
        {
            if (Settings.MapSizeExpanded)
            {
                _leftWidth = _isFlipped ? NarrowForecastWidth : ExpandedMapWidth;
                _rightWidth = _isFlipped ? ExpandedMapWidth : NarrowForecastWidth;
            }
            else
            {
                _leftWidth = _isFlipped ? NormalForecastWidth : NormalMapWidth;
                _rightWidth = _isFlipped ? NormalMapWidth : NormalForecastWidth;
            }
            LeftColumnWidth = new GridLength(_leftWidth, GridUnitType.Star);
            RightColumnWidth = new GridLength(_rightWidth, GridUnitType.Star);
                
            _currentLocation = await GetLocationAsync();

            if (await SetUpForecastAsync(_currentLocation))
            {
                if (_updateWeatherTimer == null)
                {
                    // Refresh weather automatically every 6 hours, to lower number of pings
                    _updateWeatherTimer = ThreadPoolTimer.CreatePeriodicTimer(async (t) =>
                    {
                        LogService.Write("Silently refreshing weather...");
                        _currentLocation = await GetLocationAsync();
                        InvokeOnUIThread(async () => await SetUpForecastAsync(_currentLocation, false));

                    }, TimeSpan.FromHours(6));
                }
            } 
        }

        private void AddPinToMap(SimpleLocation location)
        {
            MapLayers.Add(new MapElementsLayer
            {
                ZIndex = 1,
                MapElements = new List<MapElement>
                {
                    new MapIcon
                    {
                        Location = new Geopoint(location.Position),
                        Title = location.Name
                    }
                }
            });
        }

        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _currentLocation = await GetLocationAsync();
                await SetUpForecastAsync(_currentLocation, true);
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Gets location from Settings and if not specified, try to get current position
        /// </summary>
        private async Task<SimpleLocation> GetLocationAsync()
        {
            var location = new SimpleLocation(Common.GetLocalizedText("WeatherMapPinUnknownLabel"), 0, 0);

            try
            {
                // Immediately return unknown location if location not enabled
                if (!Settings.IsLocationEnabled)
                {
                    return location;
                }

                // If the user has specified a location, then AppSettingsVM should have validated
                // the latitude and longitude, so use that
                if ((Settings.WeatherLocationLatitude != 0 || Settings.WeatherLocationLongitude != 0) &&
                    !string.IsNullOrWhiteSpace(Settings.WeatherLocationString))
                {
                    LogService.Write($"Location has been specified in settings - " +
                        $"Name: {Settings.WeatherLocationString}, " +
                        $"Latitude: {Settings.WeatherLocationLatitude}, " +
                        $"Longitude: {Settings.WeatherLocationLongitude}");
                    location.Position.Latitude = Settings.WeatherLocationLatitude;
                    location.Position.Longitude = Settings.WeatherLocationLongitude;
                    location.Name = Settings.WeatherLocationString;
                }
                // Use current location if geolocation is allowed and a specific location wasn't specified
                else
                {
                    LogService.Write("No location specified, trying to find current location...");
                    var accessStatus = await Geolocator.RequestAccessAsync();
                    if (accessStatus == GeolocationAccessStatus.Allowed)
                    {
                        Geolocator geolocator = new Geolocator();
                        Geoposition pos = await geolocator.GetGeopositionAsync();

                        location.Position.Latitude = pos.Coordinate.Point.Position.Latitude;
                        location.Position.Longitude = pos.Coordinate.Point.Position.Longitude;

                        LogService.Write($"Geolocation Position - Latitude: {location.Position.Latitude}, Longitude: {location.Position.Longitude}");

                        // Try to find the location name using Bing Maps API - this won't work if the Map Service token isn't specified
                        LogService.Write("Attempting to find location name...");
                        var results = await MapLocationFinder.FindLocationsAtAsync(new Geopoint(location.Position));
                        if (results.Status == MapLocationFinderStatus.Success && (results.Locations.Count != 0))
                        {
                            location.Name = results.Locations[0].DisplayName;
                            LogService.Write($"Location found: {location.Name}");
                        }
                        else
                        {
                            LogService.Write($"Could not find location name ({Enum.GetName(typeof(MapLocationFinderStatus), results.Status)})");

                            // In the absence of an actual name, use the lat/long
                            location.Name = FormatCurrentLocationString(location.Position.Latitude, location.Position.Longitude);
                        }
                    }
                    else
                    {
                        LogService.Write($"Geolocation Access Status: {Enum.GetName(typeof(GeolocationAccessStatus), accessStatus)}");
                    }
                }
            }
            catch (Exception ex)
            {
                LogService.WriteException(ex);
            }

            return location;
        }

        private void RefreshWeatherUI(GenericWeather weather)
        {
            WeatherProviderString = weather.Source ?? Common.GetLocalizedText("NotAvailable");

            CurrentTemperature = GetCurrentTemperatureString(weather);
            CurrentAdditionalInfo = weather.CurrentObservation.AdditionalInfo;
            CurrentWeatherIcon = weather.CurrentObservation.Icon;
            CurrentWeather = weather.CurrentObservation.WeatherDescription;

            Forecast.Clear();
            for (int i = 0; i < 5 && i < _weather.Forecast.Days.Length; ++i)
            {
                Forecast.Add(new ForecastDay
                {
                    Day = _weather.Forecast.Days[i].DayOfWeek,
                    TempF = _weather.Forecast.Days[i].TemperatureFahrenheit,
                    TempC = _weather.Forecast.Days[i].TemperatureCelsius,
                    Icon = _weather.Forecast.Days[i].WeatherIcon,
                    Desc = _weather.Forecast.Days[i].WeatherDescription,
                });
            }
        }
        
        private void PopulateCommandBar()
        {
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            PageService?.AddCommandBarButton(PageUtil.CreatePageSettingCommandBarButton(
                PageService,
                new WeatherSettingsControl
                {
                    Width = Constants.DefaultSidePaneContentWidth,
                    Background = new SolidColorBrush(Colors.Transparent),
                },
                Common.GetLocalizedText("WeatherSettingHeader/Text")));
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            PageService?.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Refresh),
                Label = Common.GetLocalizedText("RefreshButton"),
                Handler = RefreshButton_Click,
            });
        }

        private async Task<bool> SetUpForecastAsync(SimpleLocation location, bool showLoading = true)
        {
            if (showLoading)
            {
                ShowLoadingPanel(Common.GetLocalizedText("LoadingWeatherInfoText"));
            }

            try
            {
                CurrentLocation = location.Name;

                // Set the map center
                MapCenter = new Geopoint(location.Position);

                // Add pin
                MapLayers.Clear();
                AddPinToMap(new SimpleLocation(Common.GetLocalizedText("WeatherMapPinLabel"), location.Position.Latitude, location.Position.Longitude));

                // Retrieve weather information about location
                var weather = await WeatherProvider.Instance.GetGenericWeatherAsync(location.Position.Latitude, location.Position.Longitude);

                // Update the UI if weather is available
                if (weather != null)
                {
                    // Only set weather if it's not null
                    _weather = weather;

                    // Update weather-related UI
                    RefreshWeatherUI(_weather);

                    // Try to get sensor data
                    try
                    {
                        SensorTemperatureAndHumidity = await GetSensorTemperatureAndHumidityString();
                    }
                    catch (Exception ex)
                    {
                        LogService.WriteException(ex);
                    }

                    // Show proper weather panel
                    WeatherErrorPageVisibility = false;
                    WeatherControlVisibility = true;
                    return true;
                }
                else
                {
                    // Something went wrong so show the error panel
                    WeatherErrorPageVisibility = true;
                    WeatherControlVisibility = false;
                }
            }
            finally
            {
                HideLoadingPanel();
            }
            return false;
        }

        private async void InitializeSensorServer()
        {
            if (_sensorServer == null)
            {
                _sensorServer = new SensorServer();
            }

            if (!_sensorServer.IsSensorInitialized)
            {
                await _sensorServer.InitializeAsync();
            }

            SensorEnabled = _sensorServer.IsSensorInitialized;
            SetSensorAndWeatherProperties();
        }

        public void SetSensorAndWeatherProperties()
        {
            if (SensorEnabled)
            {
                TodaysWeatherColumnProperty = 1;
                WeatherColumnSpanProperty = 1;
                SensorRowProperty = 0;
                SensorColumnSpanProperty = 1;
            }
            else
            {
                TodaysWeatherColumnProperty = 0;
                WeatherColumnSpanProperty = 2;
                SensorRowProperty = 1;
                SensorColumnSpanProperty = 2;
            }
        }

        private async void UpdateTimeAndDate(ThreadPoolTimer timer)
        {
            var now = DateTime.Now;

            string time = now.ToString("hh:mm tt");
            if (time.StartsWith("0"))
            {
                time = time.Substring(1);
            }
            CurrentTime = time;

            // Standard long date format using the current system language
            CurrentDate = now.Date.ToString("D");

            // Check to see if enough time has passed since the last reading in order to send a new message
            TimeSpan diff = now.Subtract(_latestMessageTimestamp);
            if (diff.TotalMinutes >= MessageTimeLimit)
            {
                // Upload Temperature and Humidity readings to the Azure IoT Hub, if configured
                _latestMessageTimestamp = now;
                await _sensorServer.SendSensorDataToAzureAsync();
                if (Settings.MapFlipEnabled)
                {
                    FlipDisplay();
                }
            }

            // Rough estimates for sunrise and sunset
            var sunset = DateTime.Parse("8:00 PM");
            var sunrise = DateTime.Parse("5:00 AM");
            ColorScheme = (now > sunrise && now < sunset) ? MapColorScheme.Light : MapColorScheme.Dark;
        }

        public void FlipDisplay()
        {
            if (_isFlipped)
            {
                MapColumnProperty = 0;
                WeatherColumnProperty = 1;
            }
            else
            {
                MapColumnProperty = 1;
                WeatherColumnProperty = 0;
            }
            var temp = _leftWidth;
            _leftWidth = _rightWidth;
            _rightWidth = temp;
            LeftColumnWidth = new GridLength(_leftWidth, GridUnitType.Star);
            RightColumnWidth = new GridLength(_rightWidth, GridUnitType.Star);
            _isFlipped = !_isFlipped;
        }

        #region Commands
        private RelayCommand _selectFahrenheitCommand;
        public ICommand SelectFahrenheitCommand
        {
            get
            {
                return _selectFahrenheitCommand ??
                    (_selectFahrenheitCommand = new RelayCommand(async unused =>
                    {
                        try
                        {
                            FahrenheitEnabled = !FahrenheitEnabled;
                            if (!FahrenheitEnabled && !CelsiusEnabled)
                            {
                                CelsiusEnabled = true;
                            }
                            SensorTemperatureAndHumidity = await GetSensorTemperatureAndHumidityString();
                            RefreshWeatherUI(_weather);
                        }
                        catch (Exception ex)
                        {
                            LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                        }
                    }));
            }
        }

        private RelayCommand _selectCelsiusCommand;
        public ICommand SelectCelsiusCommand
        {
            get
            {
                return _selectCelsiusCommand ??
                    (_selectCelsiusCommand = new RelayCommand(async unused =>
                    {
                        try
                        {
                            CelsiusEnabled = !CelsiusEnabled;
                            if (!FahrenheitEnabled && !CelsiusEnabled)
                            {
                                FahrenheitEnabled = true;
                            }
                            SensorTemperatureAndHumidity = await GetSensorTemperatureAndHumidityString();
                            RefreshWeatherUI(_weather);
                        }
                        catch (Exception ex)
                        {
                            LogService.Write(ex.ToString(), Windows.Foundation.Diagnostics.LoggingLevel.Error);
                        }
                    }));
            }
        }
        #endregion

        #region String Helpers
        public async Task<string> GetSensorTemperatureAndHumidityString()
        {
            string returnString = Common.GetLocalizedText("SensorUnavailableText");
            if (SensorEnabled)
            {
                returnString = string.Empty;
                SensorsData data = await _sensorServer.GetSensorDataAsync();
                if (FahrenheitEnabled)
                {
                    returnString += data.tempF + "°F - ";
                }
                if (CelsiusEnabled)
                {
                    returnString += data.tempC + "°C - ";
                }
                returnString += data.humidity + "%";
            }
            return returnString;
        }

        public string GetCurrentTemperatureString(GenericWeather weather)
        {
            string result = string.Empty;
            if (FahrenheitEnabled)
            {
                result += (int)Math.Round(weather.CurrentObservation.TemperatureFahrenheit, 0) + "°F";
            }
            if (FahrenheitEnabled && CelsiusEnabled)
            {
                result += " / ";
            }
            if (CelsiusEnabled)
            {
                result += (int)Math.Round(weather.CurrentObservation.TemperatureCelsius, 0) + "°C";
            }
            return result;
        }

        private string FormatCurrentLocationString(double latitude, double longitude)
        {
            return string.Format(Common.GetLocalizedText("WeatherLocationCoordinates"), Math.Round(latitude, 1), Math.Round(longitude, 1));
        }
        #endregion
    }
}