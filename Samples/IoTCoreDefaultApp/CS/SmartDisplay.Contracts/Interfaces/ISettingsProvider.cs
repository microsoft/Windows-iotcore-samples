// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace SmartDisplay.Contracts
{
    public interface ISettingsProvider
    {
        event TypedEventHandler<object, SettingsUpdatedEventArgs> SettingsUpdated;

        SettingType SettingType { get; }

        /// <summary>
        /// Example to store a setting named "MusicVolume":
        ///     public double MusicVolume
        ///     {
        ///         get { return settings.GetSetting(0.5); }
        ///         set { settings.SaveSetting(value); }
        ///     }
        /// </summary>
        T GetSetting<T>(T defaultValue = default(T), [CallerMemberName] string key = null);

        /// <summary>
        /// Example to store a setting named "MusicVolume":
        ///     public double MusicVolume
        ///     {
        ///         get { return settings.GetSetting(0.5); }
        ///         set { settings.SaveSetting(value); }
        ///     }
        /// </summary>
        void SaveSetting(object value, [CallerMemberName] string key = null);

        /// <summary>
        /// Returns true if the key already exists in the current settings.
        /// </summary>
        bool HasSetting([CallerMemberName] string key = null);
    }

    public enum SettingType
    {
        Local,
        Roaming
    }

    public class SettingsUpdatedEventArgs : EventArgs
    {
        public string Key;
        public object OldValue;
        public object NewValue;
    }
}
