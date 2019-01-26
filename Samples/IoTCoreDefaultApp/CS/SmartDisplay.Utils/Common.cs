// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Resources;

namespace SmartDisplay.Utils
{
    public static class Common
    {
        public static string GetLocalizedText(string key)
        {
            return ResourceLoader.GetForViewIndependentUse().GetString(key);
        }

        public static string GetAppVersion()
        {
            Package package = Package.Current;
            string packageName = package.DisplayName;
            PackageVersion version = package.Id.Version;

            return string.Format("{0} {1}.{2}.{3}.{4}", packageName, version.Major, version.Minor, version.Build, version.Revision);
        }

        public static string GetOSVersionString()
        {
            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out ulong version))
            {
                return GetLocalizedText("OSVersionNotAvailable");
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    (version & 0x000000000000FFFF));
            }
        }

        /// <summary>
        /// Try to convert the data type into a Dictionary
        /// </summary>
        /// <param name="data"></param>
        /// <returns>Dictionary object if successful, null otherwise</returns>
        public static Dictionary<string, string> ConvertToDictionary(object data)
        {
            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(JsonConvert.SerializeObject(data));
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return null;
        }

        public static bool SetPropertyByName<T>(string propertyName, object obj, object value)
        {
            try
            {
                var propInfo = typeof(T).GetProperty(propertyName);
                if (propInfo != null)
                {
                    propInfo.SetValue(obj, Convert.ChangeType(value, propInfo.PropertyType));
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }

            return false;
        }
    }
}
