//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace AudioLoopback
{
    public class AppOptions: EdgeModuleSamples.Common.Options.AppOptions
    {
        public string InputDeviceName { get; private set; }
        public string OutputDeviceName { get; private set; }

        public AppOptions()
        {
            Add<string>("in=", "Friendly {NAME} of input device", v => InputDeviceName = v);
            Add<string>("out=", "Friendly {NAME} of output device", v => OutputDeviceName = v);
        }

    }
}
