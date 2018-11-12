// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.Storage.Search;

namespace SmartDisplay.ViewModels
{
    public class MusicPlayerPageVM : BaseViewModel
    {
        #region Media file properties

        public ObservableCollection<StorageFile> MediaFileCollection
        {
            get { return GetStoredProperty<ObservableCollection<StorageFile>>(); }
            set { SetStoredProperty(value); }
        }

        public int SelectedMediaIndex
        {
            get { return GetStoredProperty<int>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Music player properties and settings

        public string MusicPlayerStatusText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool AutoPlayOn
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    Settings.MusicAutoPlay = value;
                }
            }
        }

        public bool RepeatOn
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    Settings.MusicRepeat = value;
                }
            }
        }

        public bool ShuffleOn
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    Settings.MusicShuffle = value;
                }
            }
        }

        private double _musicPlayerVolume;
        public double MusicPlayerVolume
        {
            get { return _musicPlayerVolume; }
            set
            {
                if (_musicPlayerVolume != value)
                {
                    _musicPlayerVolume = value;
                    Settings.MusicVolume = value;
                }
            }
        }

        private MediaPlaybackState _musicPlayerState;
        public MediaPlaybackState MusicPlayerState
        {
            get { return _musicPlayerState; }
            set
            {
                if (_musicPlayerState != value)
                {
                    _musicPlayerState = value;
                    RefreshUI();
                }
            }
        }

        public bool MusicPlayerVisibility
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public TimeSpan MediaDuration { get; set; }
        public TimeSpan MusicPlayerPosition { get; set; }

        #endregion

        #region Page services and providers

        public LanguageManager LanguageManager => LanguageManager.GetInstance();
        private ILogService LogService => AppService?.LogService;
        private SettingsProvider Settings => SettingsProvider.GetDefault();
        private ITelemetryService TelemetryService => AppService?.TelemetryService;
        private IMediaPlayerElementController MusicPlayerController { get; set; }

        #endregion

        #region Localized UI strings

        private string LoadingPanelText => Common.GetLocalizedText("MusicPlayerLoadingPanelText");
        private string NoMusicFoundText => Common.GetLocalizedText("MusicPlayerNoMusicFoundText");
        private string InstructionsText => Common.GetLocalizedText("MusicPlayerInstructionsText");
        private string ReloadButtonText => Common.GetLocalizedText("MusicPlayerPageReloadButtonText");
        private string CloseButtonText => Common.GetLocalizedText("CloseText");
        private string MusicPlayerCurrentlyPlayingText => Common.GetLocalizedText("MusicPlayerCurrentlyPlayingText");
        private string MusicPlayerStatusPlayingText => Common.GetLocalizedText("MusicPlayerPlayingText");
        private string MusicPlayerStatusPausedText => Common.GetLocalizedText("MusicPlayerPausedText");
        private string MusicPlayerStatusLoadingText => Common.GetLocalizedText("MusicPlayerLoadingText");
        private string MusicPlayerStatusDefaultText => Common.GetLocalizedText("MusicPlayerDefaultText");

        #endregion

        public MusicPlayerPageVM() : base()
        {
            MediaFileCollection = new ObservableCollection<StorageFile>();

            MusicPlayerVolume = Settings.MusicVolume;
            ShuffleOn = Settings.MusicShuffle;
            RepeatOn = Settings.MusicRepeat;
            AutoPlayOn = Settings.MusicAutoPlay;
        }

        internal async void SetUpVM(IMediaPlayerElementController musicPlayerView)
        {
            bool reload = true;

            while (reload)
            {
                reload = false;
                MusicPlayerVisibility = false;
                ShowLoadingPanel(LoadingPanelText);

                try
                {
                    MusicPlayerController = musicPlayerView;
                    MusicPlayerController.ClearPlaylist();

                    MediaFileCollection.Clear();

                    // Set index to -1 to indicate no selection
                    SelectedMediaIndex = -1;

                    var filesLoaded = await GetMediaFiles();

                    if (filesLoaded && MediaFileCollection.Count > 0)
                    {
                        MusicPlayerVisibility = true;
                        SelectedMediaIndex = 0;
                        UpdateMusicPlayer();
                    }
                    else
                    {
                        // Alert the user if no media files found and wait for input to reload the page or close the prompt
                        reload = await AppService.YesNoAsync(NoMusicFoundText, InstructionsText, ReloadButtonText, CloseButtonText);
                    }
                }
                catch (Exception ex)
                {
                    LogService.Write(ex.Message, LoggingLevel.Error);
                }

                HideLoadingPanel();
            }
        }

        #region Load and select media 

        private async Task<bool> GetMediaFiles()
        {
            var query = CommonFileQuery.DefaultQuery;
            var queryOptions = new QueryOptions(query, new[] { ".mp3", ".wav" })
            {
                FolderDepth = FolderDepth.Shallow
            };
            var queryResult = KnownFolders.MusicLibrary.CreateFileQueryWithOptions(queryOptions);
            var fileList = await queryResult.GetFilesAsync();

            foreach (var file in fileList)
            {
                // Add to collection for displaying list of media file sources
                MediaFileCollection.Add(file);

                // Add source to the media player's playlist
                var source = MediaSource.CreateFromStream(await file.OpenAsync(Windows.Storage.FileAccessMode.Read), file.ContentType);
                MusicPlayerController.AddToPlaylist(source);
            }
            return true;
        }

        /// <summary>
        /// Used to select a media file at a given index value within the current playlist
        /// </summary>
        /// <param name="index">The playlist index of the media file to select as an integer</param>
        /// <param name="overrideRepeat">True overrides repeat setting to select a new song, default is false</param>
        public void ChangeMusicSelection(int index, bool overrideRepeat = false)
        {
            if (SelectedMediaIndex != index)
            {
                // Update index if repeat is set to false or overrideRepeat is true when user makes a manual selection
                if (RepeatOn == false || overrideRepeat)
                {
                    SelectedMediaIndex = index;
                }

                MusicPlayerController.SelectPlaylistItem(SelectedMediaIndex);
                UpdateMusicPlayer();
            }

            // Pause the music player if autoplay setting is false
            if (MusicPlayerState == MediaPlaybackState.Playing && AutoPlayOn == false)
            {
                MusicPlayerController.Pause();
            }
        }

        #endregion

        #region Control media player and update UI

        private void UpdateMusicPlayer()
        {
            LogService.Write("Current state: " + MusicPlayerState);
            LogService.Write("Position: " + MusicPlayerPosition);
            LogService.Write("Duration: " + MediaDuration);

            var songComplete = (MediaDuration - MusicPlayerPosition) < TimeSpan.FromMilliseconds(500);

            if (MusicPlayerState == MediaPlaybackState.Paused && songComplete)
            {
                LogService.Write("Selecting the next song...");

                if (MediaFileCollection.Count > 0)
                {
                    if (SelectedMediaIndex < MediaFileCollection.Count - 1)
                    {
                        ChangeMusicSelection(SelectedMediaIndex, false);
                    }

                    if (AutoPlayOn)
                    {
                        MusicPlayerController.Play();
                    }
                }
            }
            RefreshUI();
        }

        private void RefreshUI()
        {
            InvokeOnUIThread(() =>
            {
                switch (MusicPlayerState)
                {
                    case MediaPlaybackState.Playing:
                        TelemetryService.WriteEvent("MusicPlaying");
                        MusicPlayerStatusText = MusicPlayerStatusPlayingText;
                        if (SelectedMediaIndex >= 0 && SelectedMediaIndex < MediaFileCollection.Count)
                        {
                            var selectedFile = MediaFileCollection.ElementAt(SelectedMediaIndex);
                            MusicPlayerStatusText = string.Format(Common.GetLocalizedText("MusicPlayerStatusFormat"), MusicPlayerCurrentlyPlayingText, selectedFile.DisplayName);
                        }
                        break;
                    case MediaPlaybackState.Paused:
                        MusicPlayerStatusText = MusicPlayerStatusPausedText;
                        break;
                    case MediaPlaybackState.Opening:
                        MusicPlayerStatusText = MusicPlayerStatusLoadingText;
                        break;
                    case MediaPlaybackState.Buffering:
                        MusicPlayerStatusText = MusicPlayerStatusLoadingText;
                        break;
                    default:
                        MusicPlayerStatusText = MusicPlayerStatusDefaultText;
                        break;
                }
            });
        }
    }

    #endregion
}
