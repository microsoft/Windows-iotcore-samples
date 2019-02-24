using Mono.Options;
using System;
using System.Collections.Generic;

using EdgeModuleSamples.Common;

namespace SampleModule
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public bool ShowList { get; private set; }
        public bool ShowConfig { get; private set; }
        public string DeviceId { get; private set; }
        public string DeviceAddress { get; private set; }
        public bool UseEdge { get; private set; }
        public bool Exit { get; private set; } = false;

        public AppOptions()
        {
            Add( "h|help", "show this message and exit", v => Help = v != null );
            Add( "l|list", "list available devices and exit", v => ShowList = v != null);
            Add( "d|device=", "the {ID} of i2c controller to use", v => DeviceId = v.ToLowerInvariant());
            Add( "a|address=", "the {ID} of the device address", v => DeviceAddress = v);
            Add( "c|config", "display device configuration", v => ShowConfig = v != null);
            Add( "e|edge", "transmit through azure edge", v => UseEdge = v != null);
            Add( "v|verbose", "print verbose logging information", v => Log.Verbose = v != null);
        }

        public new List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (Help || !(ShowList || ShowConfig))
            {
                Console.WriteLine($"{AppName} {AppVersion}");
                WriteOptionDescriptions(Console.Out);
                Exit = true;
            }

            return result;
        }

        static private string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }
}
