// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.ApplicationModel.Resources;

namespace SmartDisplay.Features.Utils
{
    public static class FeatureUtil
    {
        /// <summary>
        /// Utility function for retrieving resources from the SmartDisplay.Features Resource file
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetLocalizedText(string key)
        {
            return ResourceLoader.GetForViewIndependentUse().GetString("/SmartDisplay.Features/Resources/" + key);
        }
    }
}
