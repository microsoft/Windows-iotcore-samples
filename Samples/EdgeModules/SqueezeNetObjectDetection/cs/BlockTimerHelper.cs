//
// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Threading.Tasks;

using EdgeModuleSamples.Common.Logging;

namespace Helpers
{
    static class BlockTimerHelper
    {
        public static async Task<int> BlockTimer(string message,Func<Task> task)
        {
            Log.WriteLine($"{message}...");
            int ticks = Environment.TickCount;

            await task.Invoke();

            ticks = Environment.TickCount - ticks;
            Log.WriteLine($"...OK {ticks} ticks");

            return ticks;
        }
    }
}