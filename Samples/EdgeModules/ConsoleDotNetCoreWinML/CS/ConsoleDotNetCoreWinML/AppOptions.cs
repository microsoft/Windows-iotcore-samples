//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace ConsoleDotNetCoreWinML
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public bool Gpu { get; private set; }
        public bool Quiet { get; private set; }
        public bool Verbose { get; private set; }

        public AppOptions()
        {
            Add( "h|help", "show this message and exit", v => Help = v != null );
            Add("gpu", "use gpu acceleration for model evaluation", v => Gpu = v != null);
            Add("q|quiet", "suppress progress and errors to console", v => Quiet = v != null);
            Add("v|verbose", "maximum detail in console logging", v => Verbose = v != null);
        }

        public new List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (Help)
            {
                Log.WriteLine($"{AppName} {AppVersion}");
                WriteOptionDescriptions(Console.Out);
                Environment.Exit(1);
            }

            return result;
        }

        static private string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }
}
