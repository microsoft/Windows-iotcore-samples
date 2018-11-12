// Copyright (c) Microsoft Corporation. All rights reserved.

namespace SmartDisplay.Utils
{
    class Keys
    {
        // The International Weather API service is called when the device's location is outside the US or if the federal
        // government API for US weather fails.
        // This is where you can store your personal API keys.

        // By default, this app uses an API key and ID from weatherunlocked.com
        // If you would like to change the provider used, go to InternationalWeather.cs to change the query to use your
        // preferred Weather service.

        public static string WEATHER_APP_ID = "<YOUR WEATHER API ID HERE>";
        public static string WEATHER_APP_KEY = "<YOUR WEATHER API KEY HERE>";

        // Alternatively, if you don't want to rebuild the app to use your own keys, you can provide your own
        // weatherunlocked.com ID and key in a config file named {WEATHER_CONFIG_FILENAME}.
        // It has to be in the format:
        //
        // { AppId: "<YOUR WEATHER API ID HERE>", AppKey: "YOUR WEATHER API KEY HERE>"}
        // 
        // Replace everything in <> and preserve the quotes.

        // Store this file under C:\Data\Users\[User Account]\AppData\Local\Packages\[Package full name]\LocalState\
        // Example: C:\Data\Users\DefaultAccount\AppData\Local\Packages\IoTCoreDefaultAppUnderTest_1w720vyc4ccym\LocalState\

        // Filename of user provided config file
        public static string WEATHER_CONFIG_FILENAME = "WeatherToken.config";
    }
}
