// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace SmartDisplay.Controls
{
    public class WeatherSettingsControlVM : SmartDisplaySettingsBaseViewModel
    {
        #region UI and commands

        // The weather location box only works if a map service token is provided
        public bool IsWeatherLocationEnabled
        {
            get { return CanResolveLocations(); }
        }

        #endregion

        #region Settings

        public string WeatherLocationString
        {
            get { return (IsWeatherLocationEnabled) ? Settings.WeatherLocationString : Common.GetLocalizedText("MapServiceTokenRequiredTitle"); }
            set
            {
                RemoveInvalidProperty();

                Task.Run(async () =>
                {
                    var validateLocation = await ValidateLocationAsync(value);
                    if (validateLocation.Name != InvalidLocation.Name)
                    {
                        Settings.WeatherLocationString = validateLocation.Name;
                        Settings.WeatherLocationLatitude = validateLocation.Position.Latitude;
                        Settings.WeatherLocationLongitude = validateLocation.Position.Longitude;
                    }
                    else
                    {
                        Settings.WeatherLocationString = string.Empty;
                        Settings.WeatherLocationLatitude = Settings.WeatherLocationLongitude = 0;
                        AddInvalidProperty(string.Format(InvalidLocationErrorText, value));
                    }
                });
            }
        }

        public bool IsFahrenheit
        {
            get { return Settings.IsFahrenheit; }
            set { Settings.SaveSetting(value); }
        }

        #endregion

        #region Page services and providers

        #endregion

        private string InvalidLocationErrorText { get; } = Common.GetLocalizedText("InvalidLocationErrorText");
        
        private readonly SimpleLocation InvalidLocation = new SimpleLocation(null, 0, 0);
        private readonly SimpleLocation EmptyLocation = new SimpleLocation(string.Empty, 0, 0);

        private async Task<SimpleLocation> ValidateLocationAsync(string location)
        {
            LogService.Write("Validating location...");
            try
            {
                var results = await MapLocationFinder.FindLocationsAsync(location, null);

                if ((results.Status == MapLocationFinderStatus.Success) && (results.Locations.Count != 0))
                {
                    var foundLocation = results.Locations[0];
                    LogService.Write($"Found location: {foundLocation.DisplayName}");

                    return new SimpleLocation(foundLocation.DisplayName, foundLocation.Point.Position.Latitude, foundLocation.Point.Position.Longitude);
                }
                // Reset to default values so that weather page uses geolocation
                else if (string.IsNullOrWhiteSpace(location))
                {
                    LogService.Write("Location empty.");
                    return EmptyLocation;
                }
                else
                {
                    LogService.Write("Invalid location.");
                    return InvalidLocation;
                }
            }
            catch (Exception ex)
            {
                LogService.WriteException(ex);
                return InvalidLocation;
            }
        }

        private bool CanResolveLocations()
        {
            try
            {
                // Try to resolve a known working location
                var results = Task.Run(() => MapLocationFinder.FindLocationsAsync("98052", null).AsTask()).Result;
                return ((results.Status == MapLocationFinderStatus.Success) && (results.Locations.Count != 0));
            }
            catch (Exception ex)
            {
                // Catch exception if GPS or network is down
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return false;
        }
    }
}
