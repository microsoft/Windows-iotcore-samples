using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace InternetRadio
{
    internal class Playlist
    {
        public Playlist()
        {
            Tracks = new ObservableCollection<Track>();
        }
        public Playlist(string name, Guid id)
        {
            Id = id;
            Name = name;
            Tracks = new ObservableCollection<Track>();
        }

        public Guid Id;
        public string Name;
        public ObservableCollection<Track> Tracks;

        public XElement Serialize()
        {
            var xml = new XElement("Playlist");

            xml.Add(new XElement("Id", this.Id.ToString()));
            xml.Add(new XElement("Name", this.Name));

            var tracksXml = new XElement("Tracks");

            foreach(var track in this.Tracks)
            {
                tracksXml.Add(track.Serialize());
            }

            xml.Add(tracksXml);

            return xml;
        }

        public static Playlist Deserialize(XElement xml)
        {
            Playlist playlist = null;
            Guid id;

            var idElement = xml.Descendants("Id").FirstOrDefault();
            var nameElement = xml.Descendants("Name").FirstOrDefault();
            var tracksElement = xml.Descendants("Tracks").FirstOrDefault();

            if (null != nameElement &&
                null != idElement &&
                Guid.TryParse(idElement.Value, out id) &&
                null != tracksElement)
            {
                playlist = new Playlist()
                {
                    Name = nameElement.Value,
                    Id = id
                };

                foreach(var trackElement in tracksElement.Descendants("Track"))
                {
                    playlist.Tracks.Add(Track.Deserialize(trackElement));
                }
            }
            else
            {
                Debug.WriteLine("Playlist: Invalid track XML");
            }

            return playlist;
        }
    }
}
