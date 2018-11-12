// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Linq;
using Windows.Networking;
using Windows.Networking.Connectivity;

namespace SmartDisplay.Utils
{
    public static class DeviceInfoPresenter
    {
        public static string GetDeviceName()
        {
            try
            {
                var hostName = NetworkInformation.GetHostNames().FirstOrDefault(x => x.Type == HostNameType.DomainName);
                if (hostName != null)
                {
                    return hostName.DisplayName;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return $"<{Common.GetLocalizedText("NoDeviceNameText")}>";
        }

        public static string GetBoardName()
        {
            string boardName;

            switch (DeviceTypeInformation.Type)
            {
                case DeviceTypes.RPI3:
                case DeviceTypes.RPI2:
                    boardName = DeviceTypeInformation.ProductName;
                    if (string.IsNullOrEmpty(boardName))
                    {
                        boardName = Common.GetLocalizedText((DeviceTypeInformation.Type == DeviceTypes.RPI2) ? "Rpi2Name" : "Rpi3Name");
                    }
                    break;

                case DeviceTypes.MBM:
                    boardName = Common.GetLocalizedText("MBMName");
                    break;

                case DeviceTypes.DB410:
                    boardName = Common.GetLocalizedText("DB410Name");
                    break;

                default:
                    boardName = Common.GetLocalizedText("GenericBoardName");
                    break;
            }
            return boardName;
        }

        public static Uri GetBoardImageUri()
        {
            switch (DeviceTypeInformation.Type)
            {
                case DeviceTypes.RPI3:
                case DeviceTypes.RPI2:
                    return new Uri("ms-appx:///Assets/RaspberryPiBoard.png");

                case DeviceTypes.MBM:
                    return new Uri("ms-appx:///Assets/MBMBoard.png");

                case DeviceTypes.DB410:
                    return new Uri("ms-appx:///Assets/DB410Board.png");

                default:
                    return new Uri("ms-appx:///Assets/GenericBoard.png");
            }
        }
    }
}
