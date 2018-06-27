using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Windows.Security.ExchangeActiveSyncProvisioning;
using System.Reflection;

namespace Keg.DAL
{
    public static class KegLogger
    {
        private static readonly TelemetryClient telemetryClient = null;

        public enum DeviceTypes { RPI2, RPI3, MBM, DB410, GenericBoard, Unknown };
        static DeviceTypes _type = DeviceTypes.Unknown;
        static string _productName = null;
        static EasClientDeviceInformation deviceInfo;

        static KegLogger()
        {
            Init();

            //KegConfig kegConfig = GlobalSettings.GetKegSetting("fdaeadba-4027-407d-bd9a-dd679c223f65").Result;

            //Telemetry 
            TelemetryConfiguration teleConfig = new TelemetryConfiguration
            {
                InstrumentationKey = Constants.INSTRUMENTKEY
            };
            teleConfig.TelemetryChannel.DeveloperMode = false;
            if(Constants.INSTRUMENTKEY.Trim().Length == 0 )
            {
                teleConfig.DisableTelemetry = true;
            }
            //override as needed
            // true: disable telemetry, false: enables telemetry
            teleConfig.DisableTelemetry = false;
            
            telemetryClient = new TelemetryClient(teleConfig);

            telemetryClient.Context.User.Id = deviceInfo.FriendlyName;
            telemetryClient.Context.Device.Type = _type.ToString();
            telemetryClient.Context.Device.Model = _productName;
            telemetryClient.Context.Device.Id = deviceInfo.Id.ToString();
            telemetryClient.Context.Device.OemName = deviceInfo.SystemManufacturer;

            //Each reboot starts a new session
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();

            if (!ulong.TryParse(Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamilyVersion, out ulong version))
            {
                //var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                //OSVersion.Text = loader.GetString("OSVersionNotAvailable");
                telemetryClient.Context.Device.OperatingSystem = deviceInfo.OperatingSystem;
            }
            else
            {
                telemetryClient.Context.Device.OperatingSystem = String.Format(CultureInfo.InvariantCulture, "{4}-IoTCore v{0}.{1}.{2}.{3}",
                    (version & 0xFFFF000000000000) >> 48,
                    (version & 0x0000FFFF00000000) >> 32,
                    (version & 0x00000000FFFF0000) >> 16,
                    version & 0x000000000000FFFF,
                    deviceInfo.OperatingSystem);

            }

            KegLogTrace("App Started", "KegLogger", SeverityLevel.Information, null);

        }


        static void Init()
        {
            if (_type == DeviceTypes.Unknown)
            {
                deviceInfo = new EasClientDeviceInformation();
                _productName = deviceInfo.FriendlyName;

                if (deviceInfo.SystemProductName.IndexOf("MinnowBoard", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _type = DeviceTypes.MBM;
                }
                else if (deviceInfo.SystemProductName.IndexOf("Raspberry", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    if (deviceInfo.SystemProductName.IndexOf("Pi 3", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _type = DeviceTypes.RPI3;
                    }
                    else
                    {
                        _type = DeviceTypes.RPI2;
                    }
                }
                else if (deviceInfo.SystemProductName == "SBC")
                {
                    _type = DeviceTypes.DB410;
                }
                else
                {
                    _type = DeviceTypes.GenericBoard;
                }
            }

        }

        public static bool IsRaspberryPi
        {
            get
            {
                return Type == DeviceTypes.RPI2 || Type == DeviceTypes.RPI3;
            }
        }

        public static DeviceTypes Type
        {
            get
            {
                Init();
                return _type;
            }
        }

        // this might return null
        public static string ProductName
        {
            get
            {
                Init();
                return _productName;
            }
        }


        public static async void KegLogTrace(string message, string method, SeverityLevel severityLevel = SeverityLevel.Information, IDictionary<string, string> properties = null)
        {
            await Task.Run(() =>
            {
                telemetryClient.Context.Operation.Name = method;

                telemetryClient.TrackTrace(message, severityLevel, properties);
            });

        }

        public static void KegLogEvent(string message, string method, IDictionary<string, string> properties)
        {
            telemetryClient.Context.Operation.Name = method;

            telemetryClient.TrackEvent(message, properties, null);

        }

        public static void KegLogMetrics(string message, string method, string metricName, double metricValue)
        {
            var sample = new MetricTelemetry
            {
                Name = metricName,
                Sum = metricValue
            };

            sample.Context.Operation.Name = method;

            telemetryClient.Context.Operation.Name = method;

            telemetryClient.TrackMetric(sample);

        }

        public static void KegLogMetrics(string message, string method, MetricTelemetry metric)
        {
            metric.Context.Operation.Name = method;

            telemetryClient.Context.Operation.Name = method;

            telemetryClient.TrackMetric(metric);

        }


        public static void KegLogException(Exception exception, string method, IDictionary<string, string> properties)
        {
            telemetryClient.Context.Operation.Name = method;
            //exception.StackTrace = Environment.StackTrace;

            //ExceptionTelemetry exceptionTelemetry = new ExceptionTelemetry();
            //exceptionTelemetry.Context.Operation.Name = method;
            //exceptionTelemetry.Exception = exception;

            telemetryClient.TrackException(exception, properties, null);

        }

        public static void KegLogException(Exception exception, string method, SeverityLevel? severityLevel)
        {
            //telemetryClient.Context.Operation.Name = method;
            //exception.StackTrace = Environment.StackTrace;

            ExceptionTelemetry exceptionTelemetry = new ExceptionTelemetry();
            exceptionTelemetry.Context.Operation.Name = method;
            exceptionTelemetry.Exception = exception;
            exceptionTelemetry.SeverityLevel = severityLevel;
            
            telemetryClient.TrackException(exceptionTelemetry);

        }


    }
}
