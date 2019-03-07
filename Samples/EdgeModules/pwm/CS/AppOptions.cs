//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace PWMFruit
{
    public class AppOptions: EdgeModuleSamples.Common.Options.SpbAppOptions
    {
        public int? Red { get; set; }
        public int? Green { get; set; }
        public int? Yellow { get; set; }
        public int? Blue { get; set; }
        public int? Input { get; set; }

        public AppOptions()
        {
            Add<int>("r=|red=", "default pin number for red(overridable from module twin)", v => Red = v );
            Add<int>("y=|yellow=", "default pin number for yellow(overridable from module twin)", v => Yellow = v);
            Add<int>("g=|green=", "default pin number for green(overridable from module twin)", v => Green = v);
            Add<int>("b=|blue=", "default pin number for blue(overridable from module twin)", v => Blue = v);
            Add<int>("i=|input=", "default pin number for input(overridable from module twin)", v => Input = v);
        }

    }
}
