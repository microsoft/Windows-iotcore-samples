// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Foundation.Diagnostics;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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

        public string SongTitleText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string SongArtistText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public ImageSource SongThumbnailSource
        {
            get { return GetStoredProperty<ImageSource>(); }
            set { SetStoredProperty(value); }
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

        #region Commands

        private RelayCommand _reloadCommand;
        public ICommand ReloadCommand
        {
            get
            {
                return _reloadCommand ??
                    (_reloadCommand = new RelayCommand(unused =>
                    {
                        SetUpVM(MusicPlayerController);
                    }));
            }
        }

        #endregion

        private ImageSource DefaultPicture;

        public MusicPlayerPageVM() : base()
        {
            MediaFileCollection = new ObservableCollection<StorageFile>();
            MusicPlayerVisibility = true;

            MusicPlayerVolume = Settings.MusicVolume;
            ShuffleOn = Settings.MusicShuffle;
            RepeatOn = Settings.MusicRepeat;

            DefaultPicture = new BitmapImage(new Uri("ms-appx:///Assets/Images/music-icon.png"));
            SongThumbnailSource = DefaultPicture;
        }

        internal async void SetUpVM(IMediaPlayerElementController musicPlayerView)
        {
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
                    MusicPlayerVisibility = false;
                }
            }
            catch (Exception ex)
            {
                LogService.Write(ex.Message, LoggingLevel.Error);
            }

            HideLoadingPanel();
        }

        internal void TearDownVM()
        {
            _mediaInfoSemaphore.Dispose();
        }

        #region Load and select media 

        private async Task<bool> GetMediaFiles()
        {
            var fileList = await KnownFolders.MusicLibrary.GetFilesAsync();

            foreach (var file in fileList.Where(x => x.ContentType.Contains("audio")))
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
        public void ChangeMusicSelection(int index)
        {
            if (SelectedMediaIndex != index)
            {
                SelectedMediaIndex = index;

                MusicPlayerController.SelectPlaylistItem(SelectedMediaIndex);
                UpdateMusicPlayer();
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
                        ChangeMusicSelection(SelectedMediaIndex);
                    }
                }
            }
            RefreshUI();
        }

        private async void RefreshUI()
        {
            switch (MusicPlayerState)
            {
                case MediaPlaybackState.Playing:
                    TelemetryService.WriteEvent("MusicPlaying");
                    MusicPlayerStatusText = MusicPlayerStatusPlayingText;
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

            if (SelectedMediaIndex >= 0 && SelectedMediaIndex < MediaFileCollection.Count)
            {
                _currentFile = MediaFileCollection.ElementAt(SelectedMediaIndex);
                await RefreshMediaInfoAsync(_currentFile);
            }
            else
            {
                SongTitleText = string.Empty;
                SongArtistText = string.Empty;
                SongThumbnailSource = DefaultPicture;
            }
        }

        private SemaphoreSlim _mediaInfoSemaphore { get; } = new SemaphoreSlim(1, 1);
        private StorageFile _currentFile;

        private async Task RefreshMediaInfoAsync(StorageFile selectedFile)
        {
            await _mediaInfoSemaphore.WaitAsync();

            try
            {
                if (!selectedFile.Equals(_currentFile))
                {
                    LogService.Write("Selected file has changed, skipping media info refresh...");
                    return;
                }

                var musicProperties = await selectedFile.Properties.GetMusicPropertiesAsync();
                var thumbnail = await selectedFile.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.MusicView);

                if (musicProperties != null)
                {
                    SongTitleText = (string.IsNullOrWhiteSpace(musicProperties.Title)) ? selectedFile.DisplayName : musicProperties.Title;
                    SongArtistText = musicProperties.Artist;
                }

                if (thumbnail != null)
                {
                    InvokeOnUIThread(() =>
                    {
                        var bitmap = new BitmapImage();
                        bitmap.SetSource(thumbnail);
                        SongThumbnailSource = bitmap;
                    });
                }
                else
                {
                    InvokeOnUIThread(() =>
                    {
                        SongThumbnailSource = DefaultPicture;
                    });
                }
            }
            finally
            {
                _mediaInfoSemaphore.Release();
            }
        }
    }

    #endregion
}
