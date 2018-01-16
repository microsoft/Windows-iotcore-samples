using System;

namespace OnboardingServer
{
    public partial class BluetoothManager
    {
        //TODO: Change TARGET_NAME to the name of the Manager Device (a Windows 10 PC, phone, etc.) you plan to use 
        //for onboarding the IoT device through Bluetoooth.
        public const string TARGET_NAME = "<DESKTOP_NAME>";
    }

    class Constants
    {
        // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
        public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

        // The Id of the Service Name SDP attribute
        public const UInt16 SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        public const byte SdpServiceNameAttributeType = (4 << 3) | 5;

        // The value of the Service Name SDP attribute
        public static string SdpServiceName = "Bluetooth RFCOMM Chat Service";
    }
}
