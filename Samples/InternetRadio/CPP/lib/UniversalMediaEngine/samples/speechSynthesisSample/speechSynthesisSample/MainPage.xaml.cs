
using Microsoft.Maker.Media.UniversalMediaEngine;
using System;
using System.Diagnostics;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace speechSynthesisSample
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaEngine mediaEngine= new MediaEngine();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var synth = new Windows.Media.SpeechSynthesis.SpeechSynthesizer();

            var stream = await synth.SynthesizeTextToStreamAsync("Hello World");

            mediaEngine.PlayStream(stream);
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var result = await mediaEngine.InitializeAsync();
            if (MediaEngineInitializationResult.Success != result)
            {
                Debug.WriteLine("Failed to start MediaEngine");
            }
        }
    }
}
