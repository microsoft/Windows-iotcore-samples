// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace SmartDisplay.Utils
{
    public static class LocationUtil
    {
        private static ILogService LogService => ServiceUtil.LogService;

        /// <summary>
        /// Gets location from Settings and if not specified, try to get current position
        /// </summary>
        public static async Task<SimpleLocation> GetLocationAsync(ISettingsProvider settingsProvider = null)
        {
            var location = new SimpleLocation(Common.GetLocalizedText("WeatherMapPinUnknownLabel"), 0, 0);

            bool isLocationEnabled = true;
            double latitude = 0;
            double longitude = 0;
            string locationString = string.Empty;

            if (settingsProvider != null)
            {
                isLocationEnabled = settingsProvider.GetSetting(false, "IsLocationEnabled");
                latitude = settingsProvider.GetSetting(0.0, "WeatherLocationLatitude");
                longitude = settingsProvider.GetSetting(0.0, "WeatherLocationLongitude");
                locationString = settingsProvider.GetSetting(string.Empty, "WeatherLocationString");
            }

            try
            {
                // Immediately return unknown location if location not enabled
                if (!isLocationEnabled)
                {
                    return location;
                }

                // If the user has specified a location, then AppSettingsVM should have validated
                // the latitude and longitude, so use that
                if ((latitude != 0 || longitude != 0) &&
                    !string.IsNullOrWhiteSpace(locationString))
                {
                    LogService.Write($"Location has been specified in settings - " +
                        $"Name: {locationString}, " +
                        $"Latitude: {latitude}, " +
                        $"Longitude: {longitude}");
                    location.Position.Latitude = latitude;
                    location.Position.Longitude = longitude;
                    location.Name = locationString;
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

        public static string FormatCurrentLocationString(double latitude, double longitude)
        {
            return string.Format(Common.GetLocalizedText("WeatherLocationCoordinates"), Math.Round(latitude, 1), Math.Round(longitude, 1));
        }
    }

    public class SimpleLocation
    {
        public string Name;
        public BasicGeoposition Position = new BasicGeoposition();

        public SimpleLocation(string location, double latitude, double longitude)
        {
            Name = location;
            Position.Latitude = latitude;
            Position.Longitude = longitude;
        }
    }
}
