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
using Microsoft.Maker.Media.UniversalMediaEngine;
using Windows.UI.Core;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace basicMediaEngineSampleCSharp
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private MediaEngine m_mediaEngine = new MediaEngine();

        public MainPage()
        {
            this.InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            m_mediaEngine.MediaStateChanged += OnMediaStateChanged;
            await m_mediaEngine.InitializeAsync();
        }

        private async void OnMediaStateChanged(MediaState state)
        {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => CurrentStatus.Text = state.ToString());
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            m_mediaEngine.Play(UrlBox.Text);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            m_mediaEngine.Pause();
        }
    }
}
