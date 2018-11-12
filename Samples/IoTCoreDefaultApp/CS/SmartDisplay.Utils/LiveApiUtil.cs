// Copyright (c) Microsoft Corporation. All rights reserved.

using Newtonsoft.Json;
using SmartDisplay.Utils;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Identity
{
    /// <summary>
    /// Helper class for using apis.live.net
    /// </summary>
    public static class LiveApiUtil
    {
        public static async Task<LiveUserInfo> GetUserInfoAsync(string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var restUri = new Uri(@"https://apis.live.net/v5.0/me?access_token=" + token);
                    var result = await client.GetAsync(restUri);
                    if (result != null)
                    {
                        string content = await result.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<LiveUserInfo>(content);
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString());
            }

            return null;
        }

        public static async Task<BitmapImage> GetPictureAsync(string token)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    var restUri = new Uri(@"https://apis.live.net/v5.0/me/picture?access_token=" + token);
                    var result = await client.GetAsync(restUri);
                    if (result != null)
                    {
                        using (var stream = await result.Content.ReadAsStreamAsync())
                        {
                            if (stream != null)
                            {
                                using (var memStream = new MemoryStream())
                                {
                                    await stream.CopyToAsync(memStream);
                                    memStream.Position = 0;

                                    var bitmapImage = new BitmapImage();
                                    bitmapImage.SetSource(memStream.AsRandomAccessStream());

                                    return bitmapImage;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService.Write(ex.ToString());
            }

            return null;
        }
    }

    public class LiveUserInfo
    {
        public string id;
        public string name;
        public string first_name;
        public string last_name;
        public string gender;
        public string locale;
    }
}
