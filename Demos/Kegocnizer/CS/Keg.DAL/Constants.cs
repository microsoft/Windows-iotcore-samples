using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keg.DAL
{
    public static class Constants
    {
        /*
         * Keys
         */

        /// <summary>
        /// Update this with Azure Functions Url
        /// </summary>
        public const string COSMOSAzureFunctionsURL = "YOUR AZURE FUNCTIONS URL";

        /// <summary>
        /// KegConfig Guid Entiry enables to retrieve Configurations from Cloud
        /// </summary>
        public static readonly string KEGSETTINGSGUID = "YOUR KEY HERE";

        //Change this Key as required.
        public static readonly string INSTRUMENTKEY = "YOUR KEY HERE";
    }

}
