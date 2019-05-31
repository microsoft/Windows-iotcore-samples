
namespace Mfrc522Lib.Constants
{
    public static class Registers
    {
        public const byte BitFraming = 0x0D;
        public const byte ComIrq = 0x04;
        public const byte ComIrqEnable = 0x02;
        public const byte Command = 0x01;
        public const byte Control = 0x0C;
        public const byte Error = 0x06;
        public const byte FifoData = 0x09;
        public const byte FifoLevel = 0x0A;        
        public const byte Mode = 0x11;
        public const byte RxMode = 0x13;
        public const byte TimerMode = 0x2A;
        public const byte TimerPrescaler = 0x2B;
        public const byte TimerReloadHigh = 0x2C;
        public const byte TimerReloadLow = 0x2D;
        public const byte TxAsk = 0x15;
        public const byte TxControl = 0x14;
        public const byte TxMode = 0x12;
        public const byte Version = 0x37;
    }
}
