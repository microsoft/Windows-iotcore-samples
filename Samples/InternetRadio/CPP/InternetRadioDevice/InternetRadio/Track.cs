using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace InternetRadio
{
    public sealed class Track
    {
        public string Name
        {
            get; set;
        }

        public string Address
        {
            get;
            set;
        }

        internal XElement Serialize()
        {
            var xml = new XElement("Track");

            xml.Add(new XElement("Name", this.Name));
            xml.Add(new XElement("Address", this.Address));

            return xml;
        }

        internal static Track Deserialize(XElement xml)
        {
            Track track = null;

            var nameElement = xml.Descendants("Name").FirstOrDefault();
            var addressElement = xml.Descendants("Address").FirstOrDefault();

            if (null != nameElement &&
                null != addressElement)
            {
                track = new Track()
                {
                    Name = nameElement.Value,
                    Address = addressElement.Value
                };
            }
            else
            {
                Debug.WriteLine("Track: Invalid track XML");
            }

            return track;
        }
    }
}
