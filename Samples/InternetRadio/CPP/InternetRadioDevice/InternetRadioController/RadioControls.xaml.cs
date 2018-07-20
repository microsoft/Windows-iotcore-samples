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

using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using System.Diagnostics;
using Windows.UI.Popups;
using com.microsoft.maker.InternetRadio;
using Windows.Devices.AllJoyn;
using System.Collections.ObjectModel;
using System.Xml.Linq;
using System.Threading.Tasks;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InternetRadioController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class RadioControls : Page
    {
        private InternetRadioConsumer internetRadioConsumer;

        public RadioControls()
        {           
            this.InitializeComponent();
        }
   
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            var parameter = (MainPage.InternetRadioDeviceRegistrationInfo)e.Parameter;


            var joinResult = await InternetRadioConsumer.JoinSessionAsync(parameter.args, parameter.sender);

            if (joinResult.Status == AllJoynStatus.Ok)
            {
                internetRadioConsumer = joinResult.Consumer;
                var volume = await internetRadioConsumer.GetVolumeAsync();
                
                VolumeSlider.Value = (volume.Volume * 100);
                VolumeSlider.ValueChanged += VolumeSlider_ValueChanged;
                internetRadioConsumer.VolumeChanged += InternetRadioConsumer_VolumeChanged;
                internetRadioConsumer.PresetsChanged += InternetRadioConsumer_PresetsChanged;

                this.internetRadioConsumer.CurrentlyPlayingChanged += InternetRadioConsumer_CurrentlyPlayingChanged;
                await this.populateCurrentlyPlaying();
                await this.populatePresetList();

            }
        }

        private async void InternetRadioConsumer_CurrentlyPlayingChanged(InternetRadioConsumer sender, object args)
        {
            await this.populateCurrentlyPlaying();
        }

        private async Task populateCurrentlyPlaying()
        {
            var currentlyPlaying = await this.internetRadioConsumer.GetCurrentlyPlayingAsync();
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                this.CurrentlyPlaying.Text = currentlyPlaying.CurrentlyPlaying;
            });
        }

        private async void InternetRadioConsumer_PresetsChanged(InternetRadioConsumer sender, object args)
        {
            await this.populatePresetList();
        }

        private async void InternetRadioConsumer_VolumeChanged(InternetRadioConsumer sender, object args)
        {
            var volume = await internetRadioConsumer.GetVolumeAsync();
            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => 
            {
                this.VolumeSlider.Value = (volume.Volume * 100);
            });
        }

        private async Task populatePresetList()
        {
            this.PresetsList.Items.Clear();
            var presetsresult = await internetRadioConsumer.GetPresetsAsync();
            var remotePresets = XElement.Parse(presetsresult.Presets);

            foreach (var preset in remotePresets.Descendants("Track"))
            {
                var name = preset.Descendants("Name").First().Value;
                this.PresetsList.Items.Add(name);
            }
        }

        private async void VolumeSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            await internetRadioConsumer.SetVolumeAsync(e.NewValue / 100);
        }

        private async void Button_Click_1(object sender, RoutedEventArgs e)
        {
            await internetRadioConsumer.NextPresetAsync();
        }

        private async void Button_Click_2(object sender, RoutedEventArgs e)
        {
            await internetRadioConsumer.PreviousPresetAsync();
        }

        private async void Button_Click_5(object sender, RoutedEventArgs e)
        {
            var repsonse = await internetRadioConsumer.AddPresetAsync(PresetName.Text, PresetAddress.Text);

            PresetName.Text = "";
            PresetAddress.Text = "";

            if (repsonse.Status == 0)
            {
                var dialog = new MessageDialog("Preset Added");
                await dialog.ShowAsync();
            }
            else
            {
                var dialog = new MessageDialog("Failed to add preset");
                await dialog.ShowAsync();
            }
        }

        private async void Button_Click_6(object sender, RoutedEventArgs e)
        {
            var power = await internetRadioConsumer.GetPowerAsync();
            await internetRadioConsumer.SetPowerAsync(!power.Power);
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var selectedPreset = this.PresetsList.SelectedItem as string;
            if (null != selectedPreset)
            {
                await this.internetRadioConsumer.PlayPresetAsync(selectedPreset);
            }
        }

        private async void Button_Click_3(object sender, RoutedEventArgs e)
        {
            var selectedPreset = this.PresetsList.SelectedItem as string;
            if (null != selectedPreset)
            {
                await this.internetRadioConsumer.RemovePresetAsync(selectedPreset);
            }
        }
    }
}
