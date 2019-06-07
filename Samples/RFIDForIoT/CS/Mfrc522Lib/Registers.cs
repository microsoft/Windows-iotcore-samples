/*
 This code is based on the library written by github user mlowijs.
 The original library can be found here https://github.com/mlowijs/mfrc522-netmf
 */
namespace Mfrc522Lib.Constants
{
    public static class Registers
    {
        private const byte bitFraming = 0x0D;
        private const byte comIrq = 0x04;
        private const byte comIrqEnable = 0x02;
        private const byte command = 0x01;
        private const byte control = 0x0C;
        private const byte error = 0x06;
        private const byte fifoData = 0x09;
        private const byte fifoLevel = 0x0A;
        private const byte mode = 0x11;
        private const byte rxMode = 0x13;
        private const byte timerMode = 0x2A;
        private const byte timerPrescaler = 0x2B;
        private const byte timerReloadHigh = 0x2C;
        private const byte timerReloadLow = 0x2D;
        private const byte txAsk = 0x15;
        private const byte txControl = 0x14;
        private const byte txMode = 0x12;
        private const byte version = 0x37;

        public static byte BitFraming
        {
            get
            {
                return bitFraming;
            }
        }

        public static byte ComIrq
        {
            get
            {
                return comIrq;
            }
        }

        public static byte ComIrqEnable
        {
            get
            {
                return comIrqEnable;
            }
        }

        public static byte Command
        {
            get
            {
                return command;
            }
        }

        public static byte Control
        {
            get
            {
                return control;
            }
        }

        public static byte Error
        {
            get
            {
                return error;
            }
        }

        public static byte FifoData
        {
            get
            {
                return fifoData;
            }
        }

        public static byte FifoLevel
        {
            get
            {
                return fifoLevel;
            }
        }

        public static byte Mode
        {
            get
            {
                return mode;
            }
        }

        public static byte RxMode
        {
            get
            {
                return rxMode;
            }
        }

        public static byte TimerMode
        {
            get
            {
                return timerMode;
            }
        }

        public static byte TimerPrescaler
        {
            get
            {
                return timerPrescaler;
            }
        }

        public static byte TimerReloadHigh
        {
            get
            {
                return timerReloadHigh;
            }
        }

        public static byte TimerReloadLow
        {
            get
            {
                return timerReloadLow;
            }
        }

        public static byte TxAsk
        {
            get
            {
                return txAsk;
            }
        }

        public static byte TxControl
        {
            get
            {
                return txControl;
            }
        }

        public static byte TxMode
        {
            get
            {
                return txMode;
            }
        }

        public static byte Version
        {
            get
            {
                return version;
            }
        }
    }
}
