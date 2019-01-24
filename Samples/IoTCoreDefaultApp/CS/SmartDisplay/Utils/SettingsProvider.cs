// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Views;
using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;

namespace SmartDisplay.Utils
{
    /// <summary>
    /// Util for keeping track of all the settings in one place.
    /// Makes it easier to change the underlying implementation if needed.
    /// </summary>
    public class SettingsProvider : ISettingsProvider
    {
        public static readonly Color DefaultTileColor = Color.FromArgb(255, 0, 120, 215);

        #region Properties

        #region Browser

        /// <summary>
        /// Web browser's home page URL
        /// </summary>
        public string BrowserHomePage
        {
            get { return GetSetting(Constants.WODUrl); }
            set { SaveSetting(value); }
        }

        #endregion

        #region App

        /// <summary>
        /// Enables Kiosk mode, which disables and hides the navigation
        /// menu and command bar to prevent the user from leaving the page
        /// </summary>
        public bool KioskMode
        {
            get { return GetSetting<bool>(false); }
            set { SaveSetting(value); }
        }

        public bool UseMDL2Icons
        {
            get { return GetSetting<bool>(false); }
            set { SaveSetting(value); }
        }

        public bool ScreensaverEnabled
        {
            get { return GetSetting<bool>(false); }
            set { SaveSetting(value); }
        }

        public bool IsLocationEnabled
        {
            get { return GetSetting<bool>(false); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// The app will scale the UI elements to match
        /// the current screen size
        /// </summary>
        public bool AppAutoScaling
        {
            get { return GetSetting<bool>(true); }
            set { SaveSetting(value); }
        }

        public double AppScaling
        {
            // Scaling should never be less than 1
            // otherwise UI gets really messed up
            get { return Math.Max(1.0, GetSetting<double>(1.0)); }
            set { SaveSetting(value); }
        }

        public Color TileColor
        {
            get
            {
                try
                {
                    var current = GetSetting<string>();
                    if (!string.IsNullOrWhiteSpace(current))
                    {
                        byte[] bytes = Convert.FromBase64String(current);
                        return Color.FromArgb(bytes[0], bytes[1], bytes[2], bytes[3]);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                }
                return DefaultTileColor;
            }
            set
            {
                string base64 = Convert.ToBase64String(new byte[] { value.A, value.R, value.G, value.B });
                SaveSetting(base64);
            }
        }

        /// <summary>
        /// FullName of the default page type (namespace & class name).
        /// Use AppDefaultPage to get and set the settings string, but use GetDefaultPageType() to get the Type.
        /// </summary>
        public string AppDefaultPage
        {
            get { return GetSetting<string>(); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Attempts to load the type stored in AppDefaultPage.
        /// Returns a default page if the page stored in settings is unavailable.
        /// </summary>
        public Type GetDefaultPageType()
        {
            Type pageType = null;

            try
            {
                // First look for the user's saved setting.
                pageType = PageUtil.GetDescriptorFromTypeFullName(App.Settings.AppDefaultPage).Type;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            if (pageType == null)
            {
                // Look for default pages from external features.
                foreach (var feature in AppComposer.Imports.Features)
                {
                    pageType = feature.DefaultPage;
                    if (pageType != null)
                    {
                        break;
                    }
                }
            }

            if (pageType == null)
            {
                // Finally, show Device Info if there are no other default pages available.
                pageType = typeof(DeviceInfoPage);
            }

            return pageType;
        }

        /// <summary>
        /// Width of the tiles on the tile page
        /// </summary>
        public double AppTileWidth
        {
            get { return GetSetting(150.0); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Height of the tiles on the tile page
        /// </summary>
        public double AppTileHeight
        {
            get { return GetSetting(150.0); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Use MSAL for AAD authentication
        /// </summary>
        public bool AppUseMsal
        {
            get { return GetSetting<bool>(); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Allow telemetry to be sent
        /// </summary>
        public bool AppEnableTelemetry
        {
            get { return GetSetting<bool>(false); }
            set { SaveSetting(value); }
        }

        #endregion

        #region Slideshow

        /// <summary>
        /// Time before showing next image, in seconds
        /// </summary>
        public int SlideshowIntervalSeconds
        {
            get { return GetSetting(30); }
            set { SaveSetting(value); }
        }

        #endregion

        #region Weather

        /// <summary>
        /// Latitude for weather location
        /// </summary>
        public double WeatherLocationLatitude
        {
            get { return GetSetting(0.0); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Longitude for weather location
        /// </summary>
        public double WeatherLocationLongitude
        {
            get { return GetSetting(0.0); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// String name of weather location
        /// </summary>
        public string WeatherLocationString
        {
            get { return GetSetting(string.Empty); }
            set { SaveSetting(value); }
        }

        /// <summary>
        /// Temperature unit, true is fahrenheit, false is celsius
        /// </summary>
        public bool IsFahrenheit
        {
            get { return GetSetting<bool>(true); }
            set { SaveSetting(value); }
        }

        #endregion

        #region Music
        
        public bool MusicShuffle
        {
            get { return GetSetting(false); }
            set { SaveSetting(value); }
        }

        public bool MusicRepeat
        {
            get { return GetSetting(false); }
            set { SaveSetting(value); }
        }

        public double MusicVolume
        {
            get { return GetSetting(0.5); }
            set { SaveSetting(value); }
        }

        #endregion

        #region Update

        public bool AutoUpdateInstallEnabled
        {
            get { return GetSetting(true); }
            set { SaveSetting(value); }
        }

        public DateTime LastUpdateCheckUtc
        {
            // DateTime is projected as DateTimeOffset in .NET
            // Conversion is required to save to data store
            get { return GetSetting(DateTimeOffset.MinValue).UtcDateTime; }
            set{ SaveSetting(new DateTimeOffset(value)); }
        }

        public int LastUpdateCheckCount
        {
            get { return GetSetting(0); }
            set { SaveSetting(value); }
        }

        public TimeSpan ActiveTimeStart
        {
            get { return GetSetting(TimeSpan.Parse("09:00")); }
            set { SaveSetting(value); }
        }

        public TimeSpan ActiveTimeEnd
        {
            get { return GetSetting(TimeSpan.Parse("17:00")); }
            set { SaveSetting(value); }
        }

        #endregion

        #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1009:DeclareEventHandlersCorrectly", Justification = "TypedEventHandler is declared in Windows.Foundation framework")]
        public event TypedEventHandler<object, SettingsUpdatedEventArgs> SettingsUpdated;

        private ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;
        private ApplicationDataContainer _roamingSettings = ApplicationData.Current.RoamingSettings;

        private object _settingLock = new object();

        private ApplicationDataContainer CurrentSettings
        {
            get => (SettingType == SettingType.Roaming) ? _roamingSettings : _localSettings;
        }

        public SettingType SettingType { get; } = SettingType.Roaming;

        public SettingsProvider(SettingType settingType = SettingType.Roaming)
        {
            SettingType = settingType;
        }

        public static SettingsProvider GetDefault()
        {
            return new SettingsProvider();
        }

        public void SaveSetting(
            object value,
            [CallerMemberName] string key = null
            )
        {
            lock (_settingLock)
            {
                var current = CurrentSettings;
                current.Values.TryGetValue(key, out object oldValue);
                current.Values[key] = value;

                if (!Equals(value, oldValue))
                {
                    SettingsUpdated?.Invoke(this, new SettingsUpdatedEventArgs()
                    {
                        Key = key,
                        OldValue = oldValue,
                        NewValue = value
                    });
                }
            }
        }

        /// <summary>
        /// Gets the setting with the specified key. Returns defaultValue if setting doesn't exist.
        /// </summary>
        public T GetSetting<T>(
            T defaultValue = default(T),
            [CallerMemberName] string key = null
            )
        {
            lock (_settingLock)
            {
                if (CurrentSettings.Values.TryGetValue(key, out var value) && value is T tValue)
                {
                    return tValue;
                }

                return defaultValue;
            }
        }

        /// <summary>
        /// Returns true if the key already exists in the current settings.
        /// </summary>
        public bool HasSetting([CallerMemberName] string key = null)
        {
            return CurrentSettings.Values.ContainsKey(key);
        }
    }
}
