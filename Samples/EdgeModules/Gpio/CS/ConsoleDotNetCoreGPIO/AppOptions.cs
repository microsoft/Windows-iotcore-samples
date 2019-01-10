//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace ConsoleDotNetCoreGPIO
{
    public class AppOptions: OptionSet
    {
        public bool Help { get; private set; }
        public int? Red { get; set; }
        public int? Green { get; set; }
        public int? Yellow { get; set; }
        public int? Blue { get; set; }
        public int? Input { get; set; }
        public TimeSpan? Test { get; private set; }
        public bool Logging { get; private set; }
        public bool Exit { get; private set; } = false;

        public AppOptions()
        {
            Add( "h|help", "show this message and exit", v => Help = v != null );
            Add("l|log", "log progress and errors to console", v => Logging = v != null);
            Add<int>("r=|red=", "default pin number for red(overridable from module twin)", v => Red = v );
            Add<int>("y=|yellow=", "default pin number for yellow(overridable from module twin)", v => Yellow = v);
            Add<int>("g=|green=", "default pin number for green(overridable from module twin)", v => Green = v);
            Add<int>("b=|blue=", "default pin number for blue(overridable from module twin)", v => Blue = v);
            Add<int>("i=|input=", "default pin number for input(overridable from module twin)", v => Input = v);
            Add<int>("t=|test=", "seconds to test the LED hardware independently of Azure", v => Test = TimeSpan.FromSeconds(v));
        }

        public new List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (Help)
            {
                Log.WriteLine($"{AppName} {AppVersion}");
                WriteOptionDescriptions(Console.Out);
                Exit = true;
            }

            return result;
        }

        static private string AppName => typeof(AppOptions).Assembly.GetName().Name;
        static private string AppVersion => typeof(AppOptions).Assembly.GetName().Version.ToString();
    }
}
