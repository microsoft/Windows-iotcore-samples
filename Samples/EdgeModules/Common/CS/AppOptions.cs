//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace EdgeModuleSamples.Common.Options
{
    abstract public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public bool Test { get; private set; }
        public TimeSpan? TestTime { get; private set; }
        public int? TestCount { get; private set; }
        public string TestMessage { get; private set; }
        public bool Quiet { get; private set; }
        public bool Verbose { get; private set; }
        public bool List { get; private set; }


        public AppOptions()
        {
            Add("?|h|help", "show this message and exit", v => Help = v != null );
            Add("l|list", "list available devices that can be used by this sample", v => List = v != null);
            Add("q|quiet", "suppress progress and errors to console", v => Quiet = v != null);
            Add("t|test", "test without connecting to Azure", v => Test = v != null);
            Add<int>("tc=|testcount=", "test {COUNT} iterations without connecting to Azure", v => TestCount = v );
            Add<int>("tt=|testtime=", "test for {SECONDS} without connecting to Azure", v => TestTime = TimeSpan.FromSeconds(v) );
            Add<string>("tm=|testmsg=|testmessage=", "test with {MESSAGE}", v => TestMessage = v);
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

    public class SpbAppOptions : EdgeModuleSamples.Common.Options.AppOptions
    {

        // TODO: consider moving this to base
        public string DeviceName { get; private set; }

        public SpbAppOptions()
        {
            Add<string>("d=|dev=|devicename=", "Friendly {NAME} of device", v => DeviceName = v);
        }

    }

}
