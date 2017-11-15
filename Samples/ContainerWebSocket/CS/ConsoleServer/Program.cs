using Microsoft.Windows.Iot.Core;
using SharedData;
using System;

namespace IotEnterpriseServer
{
    class Program
    {
        private static object consoleLock = new object();
        static void Main(string[] args)
        {
            try
            {
                string gateway = NetworkHelper.GetDockerNAT();

                var server = new SimpleServer((s) => { lock (consoleLock) { Console.Write(s); } }, (s) => { lock (consoleLock) { Console.WriteLine(s); } });
                server.Start($"http://{gateway}:22122/wsDemo/");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
