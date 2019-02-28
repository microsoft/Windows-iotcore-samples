//
// Copyright (c) Microsoft. All rights reserved.
//
using Mono.Options;
using System;
using System.Collections.Generic;

using EdgeModuleSamples.Common.Logging;

namespace SampleModule
{
    public class AppOptions: EdgeModuleSamples.Common.Options.DeviceAppOptions
    {
        public bool ShowList { get; private set; }
        public bool ShowConfig { get; private set; }
        public bool Receive { get; private set; }
        public bool Transmit { get; private set; }
        public bool UseEdge => ! base.Test.HasValue;

        public AppOptions()
        {
            Add( "l|list", "list available devices and exit", v => ShowList = v != null);
            Add( "r|receive", "receive and display packets", v => Receive = v != null);
            Add( "s|send", "send packets (combine with -r for loopback)", v => Transmit = v != null);
            Add( "c|config", "display device configuration", v => ShowConfig = v != null);
        }
    }
}
