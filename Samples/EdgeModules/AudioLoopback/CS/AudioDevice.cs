//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common.Logging;
using AudioLoopback;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Audio;


namespace AudioLoopback
{

    public class AudioDevice : IDisposable
    {
        
        // not same as other SPB, private ctor, public static cread to allow multiple AudioController 
        // Audiocontroller class only has getdefault(). there is no getdeviceselector(friendlynamestring)
        public AudioDevice()
        {
            Log.WriteLine("Audio Device ctor complete.  controller {0} null", Device == null ? "is" : "is not");
        }
        ~AudioDevice()
        {
            Dispose(false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Device != null)
                {
                    //Device.Dispose();
                    Device = null;
                }
            }
        }

        public void Test(TimeSpan testDuration, TimeSpan pinInterval)
        {
            Log.WriteLine("Test started");
            var t = DateTime.Now;
            while (DateTime.Now - t < testDuration)
            {
                // TODO
            }
        }

    }
}
