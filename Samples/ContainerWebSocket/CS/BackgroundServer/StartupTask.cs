using Microsoft.Windows.Iot.Core;
using SharedData;
using System.Diagnostics;
using Windows.ApplicationModel.Background;

namespace Server
{
    public sealed class StartupTask : IBackgroundTask
    {
        private static object traceLock = new object();

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            string gateway = NetworkHelper.GetDockerNAT();
            var server = new SimpleServer((s) => { lock (traceLock) { Debug.Write(s); } }, (s) => { lock (traceLock) { Debug.WriteLine(s); } });
            server.Start($"http://{gateway}:22122/wsDemo/");
        }
    }
}
