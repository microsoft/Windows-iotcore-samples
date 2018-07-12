using Microsoft.Maker.Media.UniversalMediaEngine;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace InternetRadio
{
    class MediaEnginePlaybackManager : IPlaybackManager
    {
        private MediaEngine mediaEngine;
        private PlaybackState state;

        public event VolumeChangedEventHandler VolumeChanged;
        public event PlaybackStatusChangedEventHandler PlaybackStateChanged;

        public MediaEnginePlaybackManager()
        {

        }

        public double Volume
        {
            get
            {
                return this.mediaEngine.Volume;
            }
            set
            {
                if (value >= 0 && value <= 1)
                {
                    this.mediaEngine.Volume = value;
                    this.VolumeChanged(this, new VolumeChangedEventArgs() { Volume = value });
                }
                else
                {
                    Debug.WriteLine("Invalid volume entered");
                }
            }
        }

        public PlaybackState PlaybackState
        {
            get
            {
                return state;
            }
            internal set
            {
                if (this.state != value)
                {
                    this.state = value;
                    this.PlaybackStateChanged(this, new PlaybackStateChangedEventArgs() { State = this.state });
                }
            }
        }

        private void MediaEngine_MediaStateChanged(MediaState state)
        {
            switch (state)
            {
                case MediaState.Loading:
                    this.PlaybackState = PlaybackState.Loading;

                    break;

                case MediaState.Stopped:
                    this.PlaybackState = PlaybackState.Paused;

                    break;

                case MediaState.Playing:
                    this.PlaybackState = PlaybackState.Playing;
                    break;

                case MediaState.Error:
                    this.PlaybackState = PlaybackState.Error_MediaInvalid;
                    break;

                case MediaState.Ended:
                    this.PlaybackState = PlaybackState.Ended;
                    break;
            }
        }

        public async Task InitialzeAsync()
        {
            this.mediaEngine = new MediaEngine();
            var result = await this.mediaEngine.InitializeAsync();
            if (result == MediaEngineInitializationResult.Fail)
            {
                TelemetryManager.WriteTelemetryEvent("MediaEngine_FailedToInitialize");

            }

            this.mediaEngine.MediaStateChanged += MediaEngine_MediaStateChanged;
        }

        public void Play(Uri mediaAddress)
        {
            var addressString = mediaAddress.ToString();
            this.mediaEngine.Play(addressString);
        }

        public void Pause()
        {
            this.mediaEngine.Pause();
            this.PlaybackState = PlaybackState.Paused;
        }

        public void Stop()
        {
            throw new NotImplementedException();
        }
    }
}
