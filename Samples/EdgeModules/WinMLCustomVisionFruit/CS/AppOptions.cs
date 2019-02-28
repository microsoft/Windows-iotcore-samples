//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using Mono.Options;
using System;
using System.Collections.Generic;

namespace ConsoleDotNetCoreWinML
{
    public class AppOptions: EdgeModuleSamples.Common.Options.AppOptions
    {
        public bool Gpu { get; private set; }

        public AppOptions()
        {
            Add("g|gpu", "use gpu acceleration for model evaluation", v => Gpu = v != null);
        }
    }
}
