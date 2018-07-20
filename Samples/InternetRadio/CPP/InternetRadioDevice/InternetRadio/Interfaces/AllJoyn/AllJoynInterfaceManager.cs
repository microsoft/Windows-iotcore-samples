using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.AllJoyn;
using Windows.Foundation;
using Windows.ApplicationModel;
using System.Xml.Linq;
using com.microsoft.maker.InternetRadio;

namespace InternetRadio
{
    internal class AllJoynInterfaceManager : IInternetRadioService
    {
        private AllJoynBusAttachment allJoynBusAttachment;
        private InternetRadioProducer producer;
        private IPlaylistManager playlistManager;
        private IPlaybackManager playbackManager;
        private IDevicePowerManager powerManager;

        internal AllJoynInterfaceManager(IPlaybackManager playbackManager, IPlaylistManager playlistManager, IDevicePowerManager powerManager)
        {
            this.playbackManager = playbackManager;
            this.playbackManager.VolumeChanged += PlaybackManager_VolumeChanged;
            this.playlistManager = playlistManager;
            this.playlistManager.CurrentTrackChanged += PlaylistManager_CurrentTrackChanged;
            this.powerManager = powerManager;
            this.powerManager.PowerStateChanged += PowerManager_PowerStateChanged;
        }

        internal void Initialize()
        {
            this.allJoynBusAttachment = new AllJoynBusAttachment();
            this.producer = new InternetRadioProducer(this.allJoynBusAttachment);
            this.allJoynBusAttachment.AboutData.DefaultAppName = Package.Current.DisplayName;
            this.allJoynBusAttachment.AboutData.DefaultDescription = Package.Current.Description;
            this.allJoynBusAttachment.AboutData.DefaultManufacturer = Package.Current.Id.Publisher;
            this.allJoynBusAttachment.AboutData.SoftwareVersion = Package.Current.Id.Version.ToString();
            this.allJoynBusAttachment.AboutData.IsEnabled = true;
            this.producer.Service = this;
            this.producer.Start();
        }

        internal string GetBusId()
        {
            return this.allJoynBusAttachment.UniqueName;
        }

        private void PowerManager_PowerStateChanged(object sender, PowerStateChangedEventArgs e)
        {
            this.producer.EmitPowerChanged();
        }

        private void PlaylistManager_CurrentTrackChanged(object sender, PlaylistCurrentTrackChangedEventArgs e)
        {
            this.producer.EmitCurrentlyPlayingChanged();
        }

        public IAsyncOperation<InternetRadioGetCurrentlyPlayingResult> GetCurrentlyPlayingAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                return InternetRadioGetCurrentlyPlayingResult.CreateSuccessResult(
                    (null != this.playlistManager.CurrentTrack) ? this.playlistManager.CurrentTrack.Name : string.Empty);
            }).AsAsyncOperation();
        }

        private void PlaybackManager_VolumeChanged(object sender, VolumeChangedEventArgs e)
        {
            this.producer.EmitVolumeChanged();
        }

        public IAsyncOperation<InternetRadioGetVolumeResult> GetVolumeAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                return InternetRadioGetVolumeResult.CreateSuccessResult(this.playbackManager.Volume);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioSetVolumeResult> SetVolumeAsync(AllJoynMessageInfo info, double value)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                this.playbackManager.Volume = value;
                return InternetRadioSetVolumeResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioNextPresetResult> NextPresetAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
           {
               TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
               this.playlistManager.NextTrack();
               return new InternetRadioNextPresetResult();
           }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioPreviousPresetResult> PreviousPresetAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                this.playlistManager.PreviousTrack();
                return new InternetRadioPreviousPresetResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioGetVersionResult> GetVersionAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                return InternetRadioGetVersionResult.CreateSuccessResult(Package.Current.Id.Version.Major);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioAddPresetResult> AddPresetAsync(AllJoynMessageInfo info, string interfaceMemberPresetName, string interfaceMemberPresetAddress)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                var newPreset = new Track();
                newPreset.Name = interfaceMemberPresetName;
                newPreset.Address = interfaceMemberPresetAddress;
                this.playlistManager.CurrentPlaylist.Tracks.Add(newPreset);
                return InternetRadioAddPresetResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioRemovePresetResult> RemovePresetAsync(AllJoynMessageInfo info, string interfaceMemberPresetName)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                var trackPresetToDelete = this.playlistManager.CurrentPlaylist.Tracks.FirstOrDefault(t => t.Name == interfaceMemberPresetName);
                if (null != trackPresetToDelete)
                {
                    this.playlistManager.CurrentPlaylist.Tracks.Remove(trackPresetToDelete);
                }

                return InternetRadioRemovePresetResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioPlayPresetResult> PlayPresetAsync(AllJoynMessageInfo info, string interfaceMemberPresetName)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                if (this.playlistManager.PlayTrack(interfaceMemberPresetName))
                {
                    return InternetRadioPlayPresetResult.CreateSuccessResult();
                }
                else
                {
                    return InternetRadioPlayPresetResult.CreateFailureResult(-1);
                }
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioGetPresetsResult> GetPresetsAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                var presets = new XElement("Presets");

                foreach (var preset in this.playlistManager.CurrentPlaylist.Tracks)
                {
                    presets.Add(preset.Serialize());
                }

                return InternetRadioGetPresetsResult.CreateSuccessResult(presets.ToString());
            }).AsAsyncOperation();            
        }

        public IAsyncOperation<InternetRadioGetPowerResult> GetPowerAsync(AllJoynMessageInfo info)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                return InternetRadioGetPowerResult.CreateSuccessResult(PowerState.Powered == this.powerManager.PowerState);
            }).AsAsyncOperation();
        }

        public IAsyncOperation<InternetRadioSetPowerResult> SetPowerAsync(AllJoynMessageInfo info, bool value)
        {
            return Task.Run(() =>
            {
                TelemetryManager.WriteTelemetryEvent("Action_AllJoyn");
                this.powerManager.PowerState = (false) ?  PowerState.Standby : PowerState.Powered;

                return InternetRadioSetPowerResult.CreateSuccessResult();
            }).AsAsyncOperation();
        }
    }

}
