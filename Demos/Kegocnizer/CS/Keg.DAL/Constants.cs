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
         * Demo Keys
         */

        ///// <summary>
        ///// Update this with Azure Functions Url
        ///// </summary>
        //public const string COSMOSAzureFunctionsURL = "https://kegocnizerdemofunctions.azurewebsites.net/api/";

        ///// <summary>
        ///// KegConfig Guid Entiry enables to retrieve Configurations from Cloud
        ///// </summary>
        //public static readonly string KEGSETTINGSGUID = "6fd83ffd-601a-48b0-bff9-566250928e8d";

        ////Change this Key as required.
        //public static readonly string INSTRUMENTKEY = "e86b04e5-ecd0-4b44-a595-d416f0611a8b";

        /*
         * Deployed Keys
         */

        /// <summary>
        /// Update this with Azure Functions Url
        /// </summary>
        public const string COSMOSAzureFunctionsURL = "https://wsdkegfunctions.azurewebsites.net/api/";

        /// <summary>
        /// KegConfig Guid Entiry enables to retrieve Configurations from Cloud. Typically Keg Admin tool will help getting this Guid
        /// </summary>
        public static readonly string KEGSETTINGSGUID = "2a9d4c3a-75d7-4222-9827-5704efb24b54";

        //Change this Key as required.
        public static readonly string INSTRUMENTKEY = "6d8fbbea-ecf0-4827-b6ff-c79199ba869e";
    }
}
