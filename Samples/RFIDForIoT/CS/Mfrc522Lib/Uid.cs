/*
 This code is based on the library written by github user mlowijs.
 The original library can be found here https://github.com/mlowijs/mfrc522-netmf
 */
namespace Mfrc522Lib
{
    public class Uid
    {
        public byte Bcc { get; private set; }
        public byte[] Bytes { get; private set; }
        public byte[] FullUid { get; private set; }
        public bool IsValid { get; private set; }

        internal Uid(byte[] uid)
        {
            FullUid = uid;
            Bcc = uid[4];

            Bytes = new byte[4];
            System.Array.Copy(FullUid, 0, Bytes, 0, 4);

            foreach (var b in Bytes)
            {
                if (b != 0x00)
                    IsValid = true;
            }
        }

        public sealed override bool Equals(object obj)
        {
            if (!(obj is Uid))
                return false;

            var uidWrapper = (Uid)obj;

            for (int i = 0; i < 5; i++)
            {
                if (FullUid[i] != uidWrapper.FullUid[i])
                    return false;
            }

            return true;
        }

        public sealed override int GetHashCode()
        {
            int uid = 0;

            for (int i = 0; i < 4; i++)
                uid |= Bytes[i] << (i * 8);

            return uid;
        }

        public sealed override string ToString()
        {
            var formatString = "x" + (Bytes.Length * 2);
            return GetHashCode().ToString(formatString);
        }
    }
}
