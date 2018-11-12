// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.Security.ExchangeActiveSyncProvisioning;

namespace SmartDisplay.Utils
{
    public enum DeviceTypes
    {
        RPI2,
        RPI3,
        MBM,
        DB410,
        GenericBoard,
        Unknown
    };

    public static class DeviceTypeInformation
    {
        private static DeviceTypes _type = DeviceTypes.Unknown;
        private static string _productName;

        public static DeviceTypes Type
        {
            get
            {
                Init();
                return _type;
            }
        }

        public static string ProductName
        {
            get
            {
                Init();
                return _productName;
            }
        }

        public static bool IsRaspberryPi => Type == DeviceTypes.RPI2 || Type == DeviceTypes.RPI3;

        private static void Init()
        {
            if (_type == DeviceTypes.Unknown)
            {
                var deviceInfo = new EasClientDeviceInformation();
                _productName = deviceInfo.SystemProductName;
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
    }
}
