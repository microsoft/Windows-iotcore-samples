// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using Windows.UI.Xaml;

namespace SmartDisplay.Utils
{
    public static class ServiceUtil
    {
        private static ILogService _logService;
        public static ILogService LogService
        {
            get
            {
                if (_logService == null)
                {
                    if (Application.Current is IAppServiceProvider provider)
                    {
                        _logService = provider.GetForCurrentContext().LogService;
                    }
                }
                return _logService;
            }
        }

        private static ITelemetryService _telemetryService;
        public static ITelemetryService TelemetryService
        {
            get
            {
                if (_telemetryService == null)
                {
                    if (Application.Current is IAppServiceProvider provider)
                    {
                        _telemetryService = provider.GetForCurrentContext().TelemetryService;
                    }
                }
                return _telemetryService;
            }
        }
    }
}
