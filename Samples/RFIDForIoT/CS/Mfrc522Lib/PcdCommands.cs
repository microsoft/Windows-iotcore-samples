/*
 This code is based on the library written by github user mlowijs.
 The original library can be found here https://github.com/mlowijs/mfrc522-netmf
 */
namespace Mfrc522Lib.Constants
{
    public static class PcdCommands
    {
        private const byte idle = 0x00;
        private const byte mifareAuthenticate = 0x0E;
        private const byte transceive = 0x0C;

        public static byte Idle
        {
            get
            {
                return idle;
            }
        }

        public static byte MifareAuthenticate
        {
            get
            {
                return mifareAuthenticate;
            }
        }

        public static byte Transceive
        {
            get
            {
                return transceive;
            }
        }
    }
}
