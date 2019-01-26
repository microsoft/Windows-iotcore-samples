using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Maker.Devices.TextDisplay
{
    public sealed class TextDisplayConfig
    {
        public TextDisplayConfig()
        {
            DriverConfigurationValues = new Dictionary<string, string>();
        }

        public string DriverType
        {
            get;
            set;
        }

        public string DriverAssembly
        {
            get;
            set;
        }

        public uint Height
        {
            get;
            set;
        }

        public uint Width
        {
            get;
            set;
        }

        public IDictionary<string, string> DriverConfigurationValues
        {
            get;
            private set;
        }
    }
}
