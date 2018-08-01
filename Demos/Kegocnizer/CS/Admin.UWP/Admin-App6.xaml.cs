using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Net.Http;
using Keg.DAL;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Admin.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Admin_App6 : Page
    {
        private class KegConfig
        {
            [JsonProperty("maxeventdurationminutes")]
            public int MaxEventDurationMinutes { get; set; }

            [JsonProperty("maxuserouncesperhour")]
            public int MaxUserOuncesPerHour { get; set; }

            [JsonProperty("corehours")]
            public string CoreHours { get; set; }

            [JsonProperty("coredays")]
            public string CoreDays { get { return "Mon, Tue, Wed, Thu, Fri"; } }
        }
        private void PopulateComboBox()
        {
            List<string> timeIntervals = new List<string>();
            TimeSpan startTime = new TimeSpan(1, 0, 0);
            DateTime startDate = new DateTime(DateTime.MinValue.Ticks); // Date to be used to get shortTime format.
            for (int i = 0; i < 12; i++)
            {
                int minutesToBeAdded = 60 * i;      // Increasing minutes by 30 minutes interval
                TimeSpan timeToBeAdded = new TimeSpan(0, minutesToBeAdded, 0);
                TimeSpan t = startTime.Add(timeToBeAdded);
                DateTime result = startDate + t;
                timeIntervals.Add(result.ToString("HH:mm"));      // Use Date.ToShortTimeString() method to get the desired format                
            }
            cBox1.ItemsSource = timeIntervals;
            cBox1.SelectedIndex = timeIntervals.Count() / 2;
            cBox2.ItemsSource = new List<string> { "AM", "PM" };
            cBox2.SelectedIndex = 0;
            cBox3.ItemsSource = timeIntervals;
            cBox3.SelectedIndex = (timeIntervals.Count() / 2) + 4;
            cBox4.ItemsSource = new List<string> { "AM", "PM" };
            cBox4.SelectedIndex = 1;
            List<int> minutes = new List<int>();
            for(int i = 1; i < 24; i++ )
            {
                minutes.Add(i * 60);
            }
            cBox5.ItemsSource = minutes;
            cBox5.SelectedIndex = 1;
            cBox6.ItemsSource = new List<int> { 1, 2, 3, 4, 5, 6 };
            cBox6.SelectedIndex = 3;
        }
        public Admin_App6()
        {
            this.InitializeComponent();
            PopulateComboBox();
        }

        private static async void SaveKegConfigAsync(KegConfig config)
        {
            var client = new System.Net.Http.HttpClient();
            string url = $"https://kegocnizerdemofunctions.azurewebsites.net/api/kegconfig";
            var data = JsonConvert.SerializeObject(config);
            StringContent content = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
            KegLogger.KegLogTrace($"Save Keg Config MaxEventDuration : {config.MaxEventDurationMinutes} MaxUserOuncesPerHour: {config.MaxUserOuncesPerHour} CoreHours: {config.CoreHours}",
                "App6:SaveSettings", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, null);
            var result = await client.PostAsync(url, content);
        }

        private void SaveSettings(object sender, RoutedEventArgs e)
        {
            KegConfig config = new KegConfig();
            config.MaxEventDurationMinutes = (int)cBox5.SelectedItem;
            config.MaxUserOuncesPerHour = (int)cBox6.SelectedItem;
            config.CoreHours = GetCoreHours(cBox1.SelectedItem.ToString() + cBox2.SelectedItem.ToString() + ";" + cBox3.SelectedItem.ToString() + cBox4.SelectedItem.ToString());
            SaveKegConfigAsync(config);
        }

        private string GetCoreHours(String timeString)
        {
            string EventDuration = null;
            String[] timeStrings = timeString.Split(';');
            DateTime dt1 = DateTime.ParseExact(timeStrings[0], "hh:mmtt", System.Globalization.CultureInfo.CurrentCulture);
            DateTime dt2 = DateTime.ParseExact(timeStrings[1], "hh:mmtt", System.Globalization.CultureInfo.CurrentCulture);
            EventDuration = dt1.Hour + "T" + dt2.Hour;
            return EventDuration;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Admin_App2));
        }
    }
}
