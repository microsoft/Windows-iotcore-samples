using System;
using System.Threading.Tasks;

namespace Helpers
{
    static class BlockTimerHelper
    {
        public static async Task BlockTimer(string message,Func<Task> task)
        {
            Console.WriteLine($"{message}...");
            int ticks = Environment.TickCount;

            await task.Invoke();

            ticks = Environment.TickCount - ticks;
            Console.WriteLine($"...OK {ticks} ticks");

        }
    }
}