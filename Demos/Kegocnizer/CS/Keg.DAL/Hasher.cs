using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;

namespace Keg.DAL
{
    public class Hasher
    {
        public static string GetSmartCardHash(string SmartCardId)
        {
            if (SmartCardId == null)
                return SmartCardId;
            SHA256 crypt = SHA256.Create();
            var hash = new System.Text.StringBuilder();
            byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(SmartCardId));
            foreach (byte b in crypto)
            {
                hash.Append(b.ToString("x2"));
            }
            return hash.ToString();
        }
    }
}
