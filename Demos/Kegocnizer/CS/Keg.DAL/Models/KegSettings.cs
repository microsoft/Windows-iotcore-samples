using Keg.DAL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.DAL.Models
{

    /// <summary>
    /// Class Designed to hold the values from CosmosDB related to KegConfig Only
    /// </summary>
    public class KegConfig
    {
        /// <summary>
        /// Hardcoded class for KegConfig Type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get { return "KegConfig"; } }

        /// <summary>
        /// Identifier
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Maximum Event Duration In Minutes
        /// </summary>
        [JsonProperty("maxeventdurationminutes")]
        public int MaxEventDurationMinutes { get; set; }

        /// <summary>
        /// Maximum Ounces to be consumed in UserConsumptionReset Minutes
        /// </summary>
        [JsonProperty("maxuserouncesperhour")]
        public int MaxUserOuncesPerHour { get; set; }

        /// <summary>
        /// Core Business Hours ( Not allowed)
        /// </summary>
        [JsonProperty("corehours")]
        public string CoreHours { get; set; }

        /// <summary>
        /// Core Days allowed
        /// </summary>
        [JsonProperty("coredays")]
        public string CoreDays { get; set; }

        /// <summary>
        /// Maintenance Mode Flag
        /// </summary>
        [JsonProperty("maintenance")]
        public bool MaintenanceMode { get; set; }

        /// <summary>
        /// Weight Callibration Factor
        /// </summary>
        [JsonProperty("weightcalibrationfactor")]
        public float WeightCalibrationFactor { get; set; }

        /// <summary>
        /// Weight Callibration Offset
        /// </summary>
        [JsonProperty("weightcalibrationoffset")]
        public float WeightCalibrationOffset { get; set; }

        /// <summary>
        /// Maxiumum volume in the Keg
        /// </summary>
        [JsonProperty("maxkegvolumeinpints")]
        public float MaxKegVolumeInPints { get; set; }

        /// <summary>
        /// Maximum Weight Of Keg allowed
        /// </summary>
        [JsonProperty("maxkegweight")]
        public float MaxKegWeight { get; set; }

        /// <summary>
        /// Weight of Empty Keg
        /// </summary>
        [JsonProperty("emptykegweight")]
        public float EmptyKegWeight { get; set; }

        /// <summary>
        /// Maximum visitors allowed per Event
        /// </summary>
        [JsonProperty("maxvisitorsperevent")]
        public Int32 MaxPersonsPerEvent { get; set; }

        /// <summary>
        /// User Consumption Limit Reset
        /// </summary>
        [JsonProperty("userconsumptionresetminutes")]
        public Int32 UserConsumptionReset { get; set; }

        /// <summary>
        /// Flow Callibration Number
        /// </summary>
        [JsonProperty("flowcalibrationfactor")]
        public float FlowCalibrationFactor { get; set; }
        
        /// <summary>
        /// Flow Callibration Offset
        /// </summary>
        [JsonProperty("flowcalibrationoffset")]
        public float FlowCalibrationOffset { get; set; }


    }

    public static class GlobalSettings
    {
        public static string UrlCombine(params string[] urls)
        {
            string result = "";

            foreach (var url in urls)
            {
                if (result.Length > 0 && url.Length > 0)
                    result += '/';

                result += url.Trim('/');
            }

            return result;
        }

        private static bool CheckValidUrl()
        {
            try
            {
                //TODO: Add Validation
                //Constants.COSMOSAzureFunctionsURL
                return true;
            }
            catch(Exception ex)
            {
                KegLogger.KegLogException(ex, "GlobalSettings:GetKegSetting", SeverityLevel.Critical);
                throw ex;
            }           
            
        }

        public static async Task<IEnumerable<KegConfig>> GetKegSettings()
        {
            if(!CheckValidUrl())
            {
                return null;
            }
            var client = new System.Net.Http.HttpClient();
            string url =  UrlCombine(Constants.COSMOSAzureFunctionsURL,"kegconfig");
            var response = await client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();
            List<KegConfig> list = JsonConvert.DeserializeObject<List<KegConfig>>(body);
            return list;
        }

        public static async Task<KegConfig> GetKegSetting(string id)
        {
            if (!CheckValidUrl())
            {
                return null;
            }

            var myClientHandler = new HttpClientHandler
            {
                Credentials = System.Net.CredentialCache.DefaultCredentials
            };

            var client = new HttpClient(myClientHandler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            //var client = new Windows.Web.Http.HttpClient();
            string url = UrlCombine(Constants.COSMOSAzureFunctionsURL, "kegconfig", id);

            try
            {
                var response = await client.GetAsync(new Uri(url));
                var body = await response.Content.ReadAsStringAsync();
                List<KegConfig> list = JsonConvert.DeserializeObject<List<KegConfig>>(body);
                return list.FirstOrDefault();
            }
            catch(TaskCanceledException tEx)
            {
                KegLogger.KegLogException(tEx, "GlobalSettings:GetKegSetting", SeverityLevel.Critical);
                throw tEx;
            }
            catch(HttpRequestException hEx)
            {
                KegLogger.KegLogException(hEx, "GlobalSettings:GetKegSetting", SeverityLevel.Critical);
                throw hEx;
            }
        }

        /// <summary>
        /// Test Method and not to be used.
        /// </summary>
        /// <param name="item"></param>
        public static async void SetKegSettings(this KegConfig item)
        {
            var client = new System.Net.Http.HttpClient();
            string url = UrlCombine(Constants.COSMOSAzureFunctionsURL, "kegconfig");
            StringContent content = new StringContent(JsonConvert.SerializeObject(item));
            var response = await client.PostAsync(url, content);
            var body = await response.Content.ReadAsStringAsync();
        }
    }
}
