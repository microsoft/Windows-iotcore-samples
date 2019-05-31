
namespace Mfrc522Lib.Constants
{
    public static class PiccCommands
    {
        public const byte Anticollision_1 = 0x93;
        public const byte Anticollision_2 = 0x20;
        public const byte AuthenticateKeyA = 0x60;
        public const byte AuthenticateKeyB = 0x61;
        public const byte Halt_1 = 0x50;
        public const byte Halt_2 = 0x00;
        public const byte Read = 0x30;
        public const byte Request = 0x26;
        public const byte Select_1 = 0x93;
        public const byte Select_2 = 0x70;
        public const byte Write = 0xA0;
    }
}
