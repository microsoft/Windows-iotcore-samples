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
    public class User
    {
        [JsonProperty("type")]
        public string Type { get { return "KegUser"; } }

        [JsonProperty("hashcode")]
        public string HashCode { get; set; }

        [JsonProperty("isapprover")]
        public bool IsApprover { get; set; }

        public static async Task<User> GetUserByHashcode(string hashcode)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                string url = $"{GlobalSettings.UrlCombine(Constants.COSMOSAzureFunctionsURL, "keguser", hashcode)}";
                //string url = $"https://kegocnizerfunctions.azurewebsites.net/api/keguser/{hashcode}";
                var response = await client.GetAsync(url);
                var body = await response.Content.ReadAsStringAsync();
                List<User> list = JsonConvert.DeserializeObject<List<User>>(body);
                return list.FirstOrDefault();
            }
            catch(Exception ex)
            {
                KegLogger.KegLogException(ex, "User:GetUserByHashcode", SeverityLevel.Critical);
                return null;
            }
        }

        public static async void AddUserAsync(User item)
        {
            try
            {
                var client = new System.Net.Http.HttpClient();
                string url = $"{GlobalSettings.UrlCombine(Constants.COSMOSAzureFunctionsURL, "keguser")}";
                //string url = $"https://kegocnizerfunctions.azurewebsites.net/api/keguser";
                StringContent content = new StringContent(JsonConvert.SerializeObject(item));
                var response = await client.PostAsync(url, content);
                var body = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                KegLogger.KegLogException(ex, "User:AddUserAsync", SeverityLevel.Critical);
            }
        }
    }
}
