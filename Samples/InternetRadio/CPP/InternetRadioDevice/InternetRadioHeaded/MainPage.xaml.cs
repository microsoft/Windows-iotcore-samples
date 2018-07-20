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

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InternetRadio
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private static Uri RadioUri = new Uri(@"http://localhost:8001");
        private RadioManager radioManager;

        public MainPage()
        {
            this.InitializeComponent();
            this.radioManager = new RadioManager();
        }

        private void refreshButton_Click(object sender, RoutedEventArgs e)
        {
            this.webView.Source = RadioUri;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await this.radioManager.Initialize(InternetRadioConfig.GetDefault());
            this.webView.Source = RadioUri;
        }
    }
}
