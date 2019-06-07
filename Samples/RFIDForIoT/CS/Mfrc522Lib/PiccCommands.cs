/*
 This code is based on the library written by github user mlowijs.
 The original library can be found here https://github.com/mlowijs/mfrc522-netmf
 */
namespace Mfrc522Lib.Constants
{
    public static class PiccCommands
    {
        private const byte anticollision_1 = 0x93;
        private const byte anticollision_2 = 0x20;
        private const byte authenticateKeyA = 0x60;
        private const byte authenticateKeyB = 0x61;
        private const byte halt_1 = 0x50;
        private const byte halt_2 = 0x00;
        private const byte read = 0x30;
        private const byte request = 0x26;
        private const byte select_1 = 0x93;
        private const byte select_2 = 0x70;
        private const byte write = 0xA0;

        public static byte AuthenticateKeyA
        {
            get
            {
                return authenticateKeyA;
            }
        }

        public static byte AuthenticateKeyB
        {
            get
            {
                return authenticateKeyB;
            }
        }

        public static byte Halt_1
        {
            get
            {
                return halt_1;
            }
        }

        public static byte Halt_2
        {
            get
            {
                return halt_2;
            }
        }

        public static byte Read
        {
            get
            {
                return read;
            }
        }

        public static byte Request
        {
            get
            {
                return request;
            }
        }

        public static byte Select_1
        {
            get
            {
                return select_1;
            }
        }

        public static byte Select_2
        {
            get
            {
                return select_2;
            }
        }

        public static byte Write
        {
            get
            {
                return write;
            }
        }

        public static byte Anticollision_1
        {
            get
            {
                return anticollision_1;
            }
        }

        public static byte Anticollision_2
        {
            get
            {
                return anticollision_2;
            }
        }
    }
}
