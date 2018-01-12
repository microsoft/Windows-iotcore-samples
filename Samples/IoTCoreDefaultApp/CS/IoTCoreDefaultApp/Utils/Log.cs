using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Windows.Storage;

namespace IoTCoreDefaultApp.Utils
{
    internal static class Log
    {
        static private StorageFile _file;
        static private SemaphoreSlim _semaphore = new SemaphoreSlim(1);
        static public bool TraceEnterAndLeave = true;

        public static void Write(string message)
        {
            if (_file == null)
            {
                _file = ApplicationData.Current.LocalFolder.CreateFileAsync("IotCoreDefaultApp.log", CreationCollisionOption.OpenIfExists).AsTask().Result;
                Debug.WriteLine("Logging to: " + _file.Path);
            }

            string messageWithTimestamp = DateTime.Now.ToString() + " " + message + "\n";

            FileIO.AppendTextAsync(_file, messageWithTimestamp).AsTask().Wait();
            Debug.Write(messageWithTimestamp);
        }

        [Conditional("DEBUG")]
        public static void Trace(string message, [CallerMemberName] string memberName = "")
        {
            string time = DateTime.Now.ToString("HH:mm:ss.fff");
            Debug.WriteLine($"{time} [{memberName}] {message}");
        }

        [Conditional("DEBUG")]
        public static void Enter(string message="", [CallerMemberName] string memberName = "")
        {
            if (TraceEnterAndLeave)
            {
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                Debug.WriteLine($"{time} [{memberName}] ENTER {message}");
            }
        }

        [Conditional("DEBUG")]
        public static void Leave(string message = "", [CallerMemberName] string memberName = "")
        {
            if (TraceEnterAndLeave)
            {
                string time = DateTime.Now.ToString("HH:mm:ss.fff");
                Debug.WriteLine($"{time} [{memberName}] LEAVE {message}");
            }
        }

    }
}
