using System;
using System.Collections.Generic;
using Microsoft.ApplicationInsights;

namespace InternetRadio
{
    internal static class TelemetryManager
    {
        private static TelemetryClient s_telemetryClient;

        public static void Start()
        {
            if (null == s_telemetryClient)
            {
                s_telemetryClient = new TelemetryClient();
            }
        }

        public static void WriteTelemetryEvent(string eventName)
        {
            WriteTelemetryEvent(eventName, new Dictionary<string, string>());
        }

        public static void WriteTelemetryEvent(string eventName, IDictionary<string, string> properties)
        {
            if (null == s_telemetryClient)
            {
                s_telemetryClient = new TelemetryClient();
            }

            s_telemetryClient.TrackEvent(eventName, properties);
        }

        public static void WriteTelemetryException(Exception e)
        {
            WriteTelemetryException(e, new Dictionary<string, string>());
        }

        public static void WriteTelemetryException(Exception e, IDictionary<string, string> properties)
        {
            if (null == s_telemetryClient)
            {
                s_telemetryClient = new TelemetryClient();
            }

            s_telemetryClient.TrackException(e, properties);
        }
    }
}
