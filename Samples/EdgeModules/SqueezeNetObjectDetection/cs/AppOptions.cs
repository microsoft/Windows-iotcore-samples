using Mono.Options;
using System;
using System.Collections.Generic;

using EdgeModuleSamples.Common.Logging;

namespace SampleModule
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public bool ShowList { get; private set; }
        public bool RunForever { get; private set; }
        public string DeviceId { get; private set; }
        public bool UseEdge { get; private set; }
        public bool UseGpu { get; private set; }
        public string ModelPath { get; private set; }
        public bool Exit { get; private set; } = false;

        public AppOptions()
        {
            Add( "h|help", "show this message and exit", v => Help = v != null );
            Add( "l|list", "list available cameras and exit", v => ShowList = v != null);
            Add( "v|verbose", "print verbose logging information", v => Log.Verbose = v != null);
            Add( "d|device=", "the {ID} of device to connect", v => DeviceId = v);
            Add( "e|edge", "transmit through azure edge", v => UseEdge = v != null);
            Add( "m|model=", "path to model {FILE}", v => ModelPath = v);
            Add( "f|forever", "run forever", v => RunForever = v != null);
            Add( "g|gpu", "use GPU inferencing", v => UseGpu = v != null);
        }

        public new List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (Help || ( !ShowList && string.IsNullOrEmpty(ModelPath) ) )
            {
                Console.WriteLine($"{AppName} {AppVersion}");
                WriteOptionDescriptions(Console.Out);
                Exit = true;
            }

            if (ShowList)
                Exit = true;

            return result;
        }

        static public string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }
}
