using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InternetRadio
{
    internal struct PlaylistChangedEventArgs
    {
        public Playlist Playlist;
    }

    internal struct PlaylistCurrentTrackChangedEventArgs
    {
        public Track CurrentTrack;
    }

    delegate void PlaylistChangedEventHandler(object sender, PlaylistChangedEventArgs e);
    delegate void PlaylistCurrentTrackChangedEventHandler(object sender, PlaylistCurrentTrackChangedEventArgs e);

    interface IPlaylistManager
    {
        event PlaylistChangedEventHandler PlaylistChanged;
        event PlaylistCurrentTrackChangedEventHandler CurrentTrackChanged;

        /// <summary>
        /// Load the playlist with the given ID
        /// </summary>
        /// <param name="playlistId">The ID of the playlist</param>
        /// <returns>Whether the playlist was successfully loaded</returns>
        Task<bool> LoadPlayList(Guid playlistId);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name">The name of the new playlist</param>
        /// <param name="tracks">Tracks to start the playlist with</param>
        /// <param name="persist">If set to true the playlist will be able to be retrieved at
        /// a later time using the Guid, if not the playlist is disposed of after use</param>
        /// <returns>The ID of the new playlist</returns>
        Task<Guid> StartNewPlaylist(string name, List<Track> tracks, bool persist);

        Playlist CurrentPlaylist
        {
            get;
        }

        Track CurrentTrack
        {
            get;
        }

        /// <summary>
        /// Sets the current track to the named track if it exists in the playlist
        /// </summary>
        /// <param name="trackName">The name of the track</param>
        /// <returns>False if the track was not on the playlist, true if it was</returns>
        bool PlayTrack(string trackName);

        /// <summary>
        /// Moves the CurrentTrack forward one item
        /// </summary>
        /// <returns>The new CurrentTrack</returns>
        Track NextTrack();

        /// <summary>
        /// Moves the CurrentTrack back one item
        /// </summary>
        /// <returns>The new CurrentTrack</returns>
        Track PreviousTrack();
    }
}
