// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.Media.Core;

namespace SmartDisplay.ViewModels
{
    /// <summary>
    /// Interface used when communicating between a view and view model to control a media player instance
    /// </summary>
    internal interface IMediaPlayerElementController
    {
        /// <summary>
        /// Play the media player
        /// </summary>
        void Play();

        /// <summary>
        /// Pause the media player
        /// </summary>
        void Pause();

        /// <summary>
        /// Remove all sources from the playlist
        /// </summary>
        void ClearPlaylist();

        /// <summary>
        /// Add a new source to the playlist
        /// </summary>
        /// <param name="source">Media source from file</param>
        void AddToPlaylist(MediaSource source);

        /// <summary>
        /// Select the song in the playlist at the given index
        /// </summary>
        /// <param name="index">The index of the song to be selected</param>
        void SelectPlaylistItem(int index);
    }
}
