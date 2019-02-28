//
// Copyright (c) Microsoft. All rights reserved.
//
using Mono.Options;
using System;
using System.Collections.Generic;

using EdgeModuleSamples.Common.Logging;

namespace SampleModule
{
    public class AppOptions: EdgeModuleSamples.Common.Options.SpbAppOptions
    {
        public bool ShowList { get; private set; }
        public bool UseGpu { get; private set; }
        public string ModelPath { get; private set; }

        public AppOptions()
        {
            Add( "l|list", "list available cameras and exit", v => ShowList = v != null);
            Add( "m|model=", "path to model {FILE}", v => ModelPath = v);
            Add( "g|gpu", "use GPU inferencing", v => UseGpu = v != null);
        }
    }
}
