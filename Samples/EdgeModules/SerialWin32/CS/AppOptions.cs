//
// Copyright (c) Microsoft. All rights reserved.
//
using Mono.Options;
using System;
using System.Collections.Generic;

using EdgeModuleSamples.Common.Logging;

namespace SampleModule
{
    public class AppOptions: EdgeModuleSamples.Common.Options.AppOptions
    {
        public bool ShowList { get; private set; }
        public bool ShowConfig { get; private set; }
        public bool Receive { get; private set; }
        public bool Transmit { get; private set; }
        public string DeviceId { get; private set; }
        public bool UseEdge { get; private set; }

        public AppOptions()
        {
            Add( "l|list", "list available devices and exit", v => ShowList = v != null);
            Add( "d|device=", "the {ID} of device to connect", v => DeviceId = v);
            Add( "r|receive", "receive and display packets", v => Receive = v != null);
            Add( "t|transmit", "transmit packets (combine with -r for loopback)", v => Transmit = v != null);
            Add( "c|config", "display device configuration", v => ShowConfig = v != null);
            Add( "e|edge", "transmit through azure edge", v => UseEdge = v != null);
        }

    }
}
