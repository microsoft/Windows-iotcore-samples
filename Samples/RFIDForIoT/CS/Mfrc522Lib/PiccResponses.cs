
namespace Mfrc522Lib.Constants
{
    public static class PiccResponses
    {
        private const ushort answerToRequest = 0x0004;
        private const byte selectAcknowledge = 0x08;
        private const byte acknowledge = 0x0A;

        public static byte Acknowledge
        {
            get
            {
                return acknowledge;
            }
        }

        public static byte SelectAcknowledge
        {
            get
            {
                return selectAcknowledge;
            }
        }

        public static ushort AnswerToRequest
        {
            get
            {
                return answerToRequest;
            }
        }
    }
}
