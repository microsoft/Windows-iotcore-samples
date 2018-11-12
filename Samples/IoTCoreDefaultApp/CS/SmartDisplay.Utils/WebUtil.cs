// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;

namespace SmartDisplay.Utils
{
    public class WebUtil
    {
        public const int DefaultTimeOutMilliseconds = 3000;

        public static async Task<HttpResponseMessage> GetWithBearerTokenAsync(
            string address, 
            string token = null,
            int timeOutMilliseconds = DefaultTimeOutMilliseconds)
        {
            return await GetAsync(
                address, 
                new AuthenticationHeaderValue("Bearer", token), 
                timeOutMilliseconds);
        }

        public static async Task<HttpResponseMessage> PostWithBearerTokenAsync(
            string address, 
            string postBody, 
            string token = null,
            int timeOutMilliseconds = DefaultTimeOutMilliseconds)
        {
            return await PostAsync(
                address, 
                postBody, 
                new AuthenticationHeaderValue("Bearer", token), 
                timeOutMilliseconds);
        }

        public static async Task<HttpResponseMessage> GetAsync(
            string address, 
            AuthenticationHeaderValue authHeader, 
            int timeOutMilliseconds = DefaultTimeOutMilliseconds)
        {
            ServiceUtil.LogService?.Write("Performing GET request: " + address);

            try
            {
                using (var httpClient = new HttpClient())
                using (var cancel = new CancellationTokenSource(timeOutMilliseconds))
                {
                    httpClient.DefaultRequestHeaders.Authorization = authHeader;
                    return await httpClient.GetAsync(address, cancel.Token);
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public static async Task<HttpResponseMessage> PostAsync(
            string address, 
            string postBody, 
            AuthenticationHeaderValue authHeader, 
            int timeOutMilliseconds = DefaultTimeOutMilliseconds)
        {
            ServiceUtil.LogService?.Write("Performing POST request: " + address);

            try
            {
                using (var httpClient = new HttpClient())
                using (var cancel = new CancellationTokenSource(timeOutMilliseconds))
                {
                    httpClient.DefaultRequestHeaders.Authorization = authHeader;
                    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    return await httpClient.PostAsync(
                        address,
                        new StringContent(postBody, Encoding.UTF8, "application/json"),
                        cancel.Token);
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public static async Task<string> SendRequestAsync(string uri, string userName, string password, HttpMethod method)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var request = new HttpRequestMessage(method, uri);
                    if (userName != null && password != null)
                    {
                        string encodedAuth = Convert.ToBase64String(Encoding.GetEncoding("ISO-8859-1").GetBytes(userName + ":" + password));
                        request.Headers.Add("Authorization", "Basic " + encodedAuth);
                    }
                    request.Content = new StringContent("");
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
                    request.Content.Headers.ContentLength = 0;

                    var response = await httpClient.SendAsync(request);
                    return await response.Content.ReadAsStringAsync();
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public static async Task<HttpResponseMessage> GetResponseAsync(string uri, int timeOutMilliseconds = DefaultTimeOutMilliseconds)
        {
            try
            {
                using (var httpClient = new HttpClient())
                using (var cancel = new CancellationTokenSource(timeOutMilliseconds))
                {
                    return await httpClient.GetAsync(uri, cancel.Token);
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }
    }
}
