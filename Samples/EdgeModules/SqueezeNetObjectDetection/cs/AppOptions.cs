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
        public bool RunForever { get; private set; }
        public string DeviceId { get; private set; }
        public bool UseEdge { get; private set; }
        public bool UseGpu { get; private set; }
        public string ModelPath { get; private set; }

        public AppOptions()
        {
            Add( "d|device=", "the {ID} of device to connect", v => DeviceId = v);
            Add( "e|edge", "transmit through azure edge", v => UseEdge = v != null);
            Add( "m|model=", "path to model {FILE}", v => ModelPath = v);
            Add( "f|forever", "run forever", v => RunForever = v != null);
            Add( "g|gpu", "use GPU inferencing", v => UseGpu = v != null);
        }

        public override List<string> Parse(IEnumerable<string> args)
        {
            var result = base.Parse(args);

            if (!List && string.IsNullOrEmpty(ModelPath))
            {
                        Log.WriteLine($"{AppName} {AppVersion}");
                        WriteOptionDescriptions(Console.Out);
                        Environment.Exit(1);
            }

            return result;
        }


    }
}
