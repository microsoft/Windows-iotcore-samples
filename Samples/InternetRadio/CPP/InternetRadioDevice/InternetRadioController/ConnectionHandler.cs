using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace InternetRadioController
{
    public class ConnectionHandler
    {
        private StreamSocket socket = new StreamSocket();

        public async Task Connect(string ip, string port)
        {
            socket.Control.KeepAlive = true;
            socket.Control.NoDelay = true;
            await socket.ConnectAsync(new HostName(ip), port);
        }

        public void Disconnect()
        {
            socket.Dispose();
        }
    }
}
