// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using System.ComponentModel;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// A simple music player
    /// </summary>
    public sealed partial class MusicPlayerPage : PageBase, IMediaPlayerElementController
    {
        public MusicPlayerPageVM ViewModel { get; } = new MusicPlayerPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        private MediaPlayer MusicPlayer { get; set; }
        private MediaPlaybackList MusicPlaylist { get; set; }

        public MusicPlayerPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (MusicPlayer == null)
            {
                // Setup playlist and media player
                MusicPlaylist = new MediaPlaybackList
                {
                    AutoRepeatEnabled = ViewModel.RepeatOn,
                    ShuffleEnabled = ViewModel.ShuffleOn,
                    MaxPlayedItemsToKeepOpen = 3
                };

                MusicPlayer = new MediaPlayer
                {
                    Volume = ViewModel.MusicPlayerVolume,
                    AutoPlay = true,
                    Source = MusicPlaylist,
                };

                musicPlayerElement.SetMediaPlayer(MusicPlayer);
                compactPlayerElement.SetMediaPlayer(MusicPlayer);
            }

            ViewModel.PropertyChanged += ViewModel_PropertyChanged;
            MusicPlayer.VolumeChanged += MusicPlayer_VolumeChanged;
            MusicPlayer.PlaybackSession.PlaybackStateChanged += MusicPlaybackSession_PlaybackStateChanged;
            MusicPlaylist.CurrentItemChanged += MusicPlaybackList_CurrentItemChanged;

            ViewModel.SetUpVM(this);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            ViewModel.PropertyChanged -= ViewModel_PropertyChanged;
            MusicPlayer.VolumeChanged -= MusicPlayer_VolumeChanged;
            MusicPlayer.PlaybackSession.PlaybackStateChanged -= MusicPlaybackSession_PlaybackStateChanged;
            MusicPlaylist.CurrentItemChanged -= MusicPlaybackList_CurrentItemChanged;

            ClearPlaylist();

            // Set the bound media player to null and dispose the MusicPlayer
            musicPlayerElement.SetMediaPlayer(null);
            compactPlayerElement.SetMediaPlayer(null);

            MusicPlaylist = null;
            MusicPlayer?.Dispose();
            MusicPlayer = null;

            ViewModel.TearDownVM();
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Route UI toggle changes
            switch (e.PropertyName)
            {
                case "ShuffleOn":
                    MusicPlaylist.ShuffleEnabled = ViewModel.ShuffleOn;
                    break;
                case "RepeatOn":
                    MusicPlaylist.AutoRepeatEnabled = ViewModel.RepeatOn;
                    break;
                default:
                    return;
            }
        }

        #region Music player media selection

        private void MusicPlaybackList_CurrentItemChanged(object sender, CurrentMediaPlaybackItemChangedEventArgs e)
        {
            if (sender is MediaPlaybackList playlist)
            {
                ViewModel.ChangeMusicSelection((int)playlist.CurrentItemIndex);
            }
        }

        private void MediaListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListView list)
            {
                // If change prompted by user selection from list set overrideRepeat to true 
                ViewModel.ChangeMusicSelection(list.SelectedIndex);
            }
        }
        #endregion

        #region Music player events

        private void MusicPlayer_VolumeChanged(object sender, object e)
        {
            if (sender is MediaPlayer musicPlayer)
            {
                ViewModel.MusicPlayerVolume = musicPlayer.Volume;
            }
        }

        private void MusicPlaybackSession_PlaybackStateChanged(object sender, object e)
        {
            if (sender is MediaPlaybackSession session)
            {
                ViewModel.MusicPlayerPosition = session.Position;
                ViewModel.MediaDuration = session.NaturalDuration.Duration();
                ViewModel.MusicPlayerState = session.PlaybackState;
            }
        }
        #endregion

        #region Music player controls

        public MediaPlaybackList PlaybackList => MusicPlaylist;

        public void Play()
        {
            if (MusicPlayer?.PlaybackSession.PlaybackState != MediaPlaybackState.Playing)
            {
                MusicPlayer.Play();
            }
        }

        public void Pause()
        {
            if (MusicPlayer?.PlaybackSession.PlaybackState != MediaPlaybackState.Paused)
            {
                MusicPlayer.Pause();
            }
        }

        public void ClearPlaylist()
        {
            if (MusicPlaylist == null)
            {
                return;
            }

            MusicPlaylist.Items.Clear();
        }

        public void AddToPlaylist(MediaSource source)
        {
            if (MusicPlaylist == null || source == null)
            {
                return;
            }

            MusicPlaylist.Items.Add(new MediaPlaybackItem(source));
        }

        public void SelectPlaylistItem(int index)
        {
            if (MusicPlaylist == null)
            {
                return;
            }

            // Move the playlist to the selected track if the given index is valid
            if (index >= 0 && index < MusicPlaylist.Items.Count)
            {
                MusicPlaylist.MoveTo((uint)index);
            }
        }
        #endregion

        private void ScrollViewer_ViewChanging(object sender, ScrollViewerViewChangingEventArgs e)
        {
            // Show compact media controls when the main controls scroll off screen
            compactPlayerElement.Visibility = (e.NextView.VerticalOffset > controlStackPanel.ActualHeight) ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}
