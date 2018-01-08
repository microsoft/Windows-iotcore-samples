using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Windows.Storage;

namespace IoTCoreDefaultApp.Utils
{
    internal static class Log
    {
        static private StorageFile _file;
        static private SemaphoreSlim _semaphore = new SemaphoreSlim(1);

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
    }
}
