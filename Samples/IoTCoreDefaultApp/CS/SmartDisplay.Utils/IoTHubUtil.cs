// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json.Linq;
using System;

namespace SmartDisplay.Utils
{
    public static class IoTHubUtil
    {
        public static string ConvertPropertyToString(object value)
        {
            try
            {
                return (value as JObject)["value"].ToString();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
