using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maker.Devices.TextDisplay;
using System.Collections.Generic;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;

namespace InternetRadio
{
    public sealed class RadioManager
    {
        private IPlaylistManager radioPresetManager;
        private IPlaybackManager radioPlaybackManager;
        private IDevicePowerManager radioPowerManager;
        private ITextDisplay display;
        private ResourceLoader resourceLoader;

        private AllJoynInterfaceManager allJoynInterfaceManager;
        private GpioInterfaceManager gpioInterfaceManager;
        private HttpInterfaceManager httpInterfaceManager;

        private InternetRadioConfig config;

        private uint playbackRetries;
        private const uint maxRetries = 3;

        public IAsyncAction Initialize(InternetRadioConfig config)
        {
            return Task.Run(async () =>
            {
                this.config = config;
                this.playbackRetries = 0;

                var telemetryInitializeProperties = new Dictionary<string, string>();
#pragma warning disable CS0618 // No current view for Background task
                this.resourceLoader = new ResourceLoader("Resources");
#pragma warning restore CS0618 // No current view for Background task           

                this.radioPowerManager = new RadioPowerManager();
                this.radioPowerManager.PowerStateChanged += RadioPowerManager_PowerStateChanged;

                this.radioPresetManager = new RadioLocalPresetManager();
                this.radioPresetManager.PlaylistChanged += RadioPresetManager_PlaylistChanged;
                this.radioPresetManager.CurrentTrackChanged += RadioPresetManager_CurrentTrackChanged;

                this.radioPlaybackManager = new MediaEnginePlaybackManager();
                this.radioPlaybackManager.VolumeChanged += RadioPlaybackManager_VolumeChanged;
                this.radioPlaybackManager.PlaybackStateChanged += RadioPlaybackManager_PlaybackStateChanged;
                await this.radioPlaybackManager.InitialzeAsync();

                // Initialize the input managers

                // AllJoyn
                this.allJoynInterfaceManager = new AllJoynInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
                this.allJoynInterfaceManager.Initialize();

                // GPIO
                this.gpioInterfaceManager = new GpioInterfaceManager(this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
                if (!this.gpioInterfaceManager.Initialize(this.config.Buttons_Debounce, this.config.Buttons_Pins))
                {
                    Debug.WriteLine("RadioManager: Failed to initialize GPIO");
                    telemetryInitializeProperties.Add("GpioAvailable", false.ToString());
                }
                else
                {
                    telemetryInitializeProperties.Add("GpioAvailable", true.ToString());
                }

                // HTTP
                this.httpInterfaceManager = new HttpInterfaceManager(8001, this.radioPlaybackManager, this.radioPresetManager, this.radioPowerManager);
                this.httpInterfaceManager.StartServer();

                // Manage settings
                this.radioPlaybackManager.Volume = this.loadVolume();
                var previousPlaylist = this.loadPlaylistId();
                if (previousPlaylist.HasValue)
                {
                    await this.radioPresetManager.LoadPlayList(previousPlaylist.Value);
                    telemetryInitializeProperties.Add("FirstBoot", false.ToString());
                }
                else
                {
                    telemetryInitializeProperties.Add("FirstBoot", true.ToString());
                }

                if (this.radioPresetManager.CurrentPlaylist == null)
                {
                    var newPlaylistId = await this.radioPresetManager.StartNewPlaylist("DefaultPlaylist", new List<Track>(), true);
                    this.savePlaylistId(newPlaylistId);
                }

                var displays = await TextDisplayManager.GetDisplays();
                this.display = displays.FirstOrDefault();
                if (null != this.display)
                {
                    telemetryInitializeProperties.Add("DisplayAvailable", true.ToString());
                    telemetryInitializeProperties.Add("DisplayHeight", this.display.Height.ToString());
                    telemetryInitializeProperties.Add("DisplayWidth", this.display.Width.ToString());
                }
                else
                {
                    Debug.WriteLine("RadioManager: No displays available");
                    telemetryInitializeProperties.Add("DisplayAvailable", false.ToString());
                }

                // Wake up the radio
                this.radioPowerManager.PowerState = PowerState.Powered;

                if (this.radioPresetManager.CurrentPlaylist.Tracks.Count <= 0)
                {
                    this.radioPresetManager.CurrentPlaylist.Tracks.Add(this.config.Playlist_BuiltInStation);
                }

                TelemetryManager.WriteTelemetryEvent("App_Initialize", telemetryInitializeProperties);
            }).AsAsyncAction();
        }

        private async void RadioPowerManager_PowerStateChanged(object sender, PowerStateChangedEventArgs e)
        {
            switch (e.PowerState)
            {
                case PowerState.Powered:

                    await this.tryWriteToDisplay(this.resourceLoader.GetString("StartupMessageLine1") +
                                                        "\n" +
                                                        this.resourceLoader.GetString("StartupMessageLine2"),
                                                        0);

                    await Task.Delay(this.config.Messages_StartupMessageDelay);
                    if (null != this.radioPresetManager.CurrentTrack)
                    {
                        playChannel(this.radioPresetManager.CurrentTrack);
                    }
                    break;
                case PowerState.Standby:
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("ShutdownMessage"), 0);
                    this.radioPlaybackManager.Pause();
                    await Task.Delay(this.config.Messages_StartupMessageDelay);
                    await this.tryWriteToDisplay(String.Empty + "\n" + String.Empty, 0);
                    break;
            }
        }

        private async void RadioPlaybackManager_PlaybackStateChanged(object sender, PlaybackStateChangedEventArgs e)
        {
            Debug.WriteLine(string.Format("playbackstate changed: {0}", e.State.ToString()));
            switch (e.State)
            {
                case PlaybackState.Error_MediaInvalid:
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("MediaErrorMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Loading:
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("MediaLoadingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;

                case PlaybackState.Playing:
                    playbackRetries = 0;
                    await this.tryWriteToDisplay(this.resourceLoader.GetString("NowPlayingMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    break;
                case PlaybackState.Ended:
                    if (maxRetries > playbackRetries)
                    {
                        playChannel(this.radioPresetManager.CurrentTrack);
                    }
                    else
                    {
                        await this.tryWriteToDisplay(this.resourceLoader.GetString("ConnectionFailedMessage") + "\n" + this.radioPresetManager.CurrentTrack.Name, 0);
                    }

                    break;
            }
        }

        private async void RadioPlaybackManager_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            await this.tryWriteToDisplay(this.resourceLoader.GetString("VolumeMesage") + "\n" + ((int)(e.Volume * 100)).ToString() + "%", 3);
            this.saveVolume(e.Volume);
        }

        private void RadioPresetManager_CurrentTrackChanged(object sender, PlaylistCurrentTrackChangedEventArgs e)
        {
            playChannel(e.CurrentTrack);
        }

        private void RadioPresetManager_PlaylistChanged(object sender, PlaylistChangedEventArgs e)
        {
        }

        public IAsyncAction Dispose()
        {
            return Task.Run(async () =>
            {
                if (null != this.display)
                    await this.display.DisposeAsync();
            }).AsAsyncAction();
        }

        private void playChannel(Track track)
        {
            if (null == track)
            {
                Debug.WriteLine("RadioManager: Play Track failed due to null track");
                return;
            }
            Debug.WriteLine("RadioManager: Play Track - " + track.Name);
            this.radioPlaybackManager.Play(new Uri(track.Address));
        }

        private async Task tryWriteToDisplay(string message, uint timeout)
        {
            if (null != this.display)
            {
                await this.display.WriteMessageAsync(message, timeout);
            }

            Debug.WriteLine("RadioManager: Display - " + message);
        }

        private void saveVolume(double volume)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["volume"] = volume;
        }

        private double loadVolume()
        {
            double volume = 0;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("volume"))
            {
                volume = Convert.ToDouble(localSettings.Values["volume"]);
            }
            else
            {
                volume = .25;
            }

            return volume;
        }

        private void savePlaylistId(Guid? id)
        {
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values["playlist"] = id;
        }

        private Guid? loadPlaylistId()
        {
            Guid? playlistId = null;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            if (localSettings.Values.ContainsKey("playlist"))
            {
                playlistId = localSettings.Values["playlist"] as Guid?;
            }

            return playlistId;
        }
    }
}
