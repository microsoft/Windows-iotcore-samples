//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace EdgeModuleSamples.Common.Options
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public TimeSpan? Test { get; private set; }
        public bool Quiet { get; private set; }
        public bool Verbose { get; private set; }


        public AppOptions()
        {
            Add( "?|h|help", "show this message and exit", v => Help = v != null );
            Add("q|quiet", "suppress progress and errors to console", v => Quiet = v != null);
            Add<int>("t=|test=", "test for {SECONDS} without connecting to Azure", v => Test = TimeSpan.FromSeconds(v));
            Add("v|verbose", "maximum detail in console logging", v => Verbose = v != null);
        }

        public virtual new List<string> Parse(IEnumerable<string> args)
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

        static public string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static public string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }

    public class DeviceAppOptions : EdgeModuleSamples.Common.Options.AppOptions
    {

        public string DeviceName { get; private set; }

        public DeviceAppOptions()
        {
            Add<string>("d=|dev=|devicename=", "friendly {NAME} of device", v => DeviceName = v);
        }

    }

}
