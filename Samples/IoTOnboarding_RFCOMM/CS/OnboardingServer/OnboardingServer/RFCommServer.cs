using System;
using System.Diagnostics;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace OnboardingServer
{
    class RFCommServer
    {
        private StreamSocket socket;
        private DataWriter writer;
        private RfcommServiceProvider rfcommProvider;
        private StreamSocketListener socketListener;
        private MainPage rootPage;

        public bool isListening = false;

        public event EventHandler ClientConnected;
        protected virtual void OnClientConnected(EventArgs e)
        {
            EventHandler handler = ClientConnected;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler ClientDisonnected;
        protected virtual void OnClientDisonnected(EventArgs e)
        {
            EventHandler handler = ClientDisonnected;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        protected virtual void OnMessageReceived(MessageReceivedEventArgs e)
        {
            EventHandler<MessageReceivedEventArgs> handler = MessageReceived;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        public RFCommServer(MainPage mainPage)
        {
            rootPage = mainPage;
        }

        public void StartListening()
        {
            InitializeRfcommServer();
        }

        /// <summary>
        /// Initializes the server using RfcommServiceProvider to advertise the Chat Service UUID and start listening
        /// for incoming connections.
        /// </summary>
        private async void InitializeRfcommServer()
        {
            rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid));

            // Create a listener for this service and start listening
            socketListener = new StreamSocketListener();
            socketListener.ConnectionReceived += OnConnectionReceived;

            await socketListener.BindServiceNameAsync(rfcommProvider.ServiceId.AsString(), SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(rfcommProvider);
            rfcommProvider.StartAdvertising(socketListener);

            rootPage.Log("RFCOMM_Server::Listening for incoming connections");
            isListening = true;
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(Constants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)Constants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(Constants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(Constants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        /// <summary>
        /// Sends message to the Client.
        /// </summary>
        /// <param name="message"></param>
        public async void SendMessage(string message)
        {
            // There's no need to send a zero length message
            if (message.Length != 0)
            {
                // Make sure that the connection is still up and there is a message to send
                if (socket != null)
                {
                    writer.WriteUInt32((uint)message.Length);
                    writer.WriteString(message);

                    rootPage.Log("RFCOMM_Server::Sent: " + message);
                    // Clear the messageTextBox for a new message
                    message = "";

                    await writer.StoreAsync();
                }
                else
                {
                    rootPage.Log("RFCOMM_Server::No clients connected, please wait for a client to connect before attempting to send a message");
                }
            }
        }

        /// <summary>
        /// Clean up for disconnection.
        /// </summary>
        public void Disconnect()
        {
            if (rfcommProvider != null)
            {
                rfcommProvider.StopAdvertising();
                rfcommProvider = null;
            }

            if (socketListener != null)
            {
                socketListener.Dispose();
                socketListener = null;
            }

            if (writer != null)
            {
                writer.DetachStream();
                writer = null;
            }

            if (socket != null)
            {
                socket.Dispose();
                socket = null;
            }

            isListening = false;
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private async void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Stop advertising/listening so that we're only serving one client
            socketListener.Dispose();
            socketListener = null;
            socket = args.Socket;

            writer = new DataWriter(socket.OutputStream);

            var reader = new DataReader(socket.InputStream);
            bool remoteDisconnection = false;

            await rootPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                rootPage.Log("RFCOMM_Server::Client Connected");
                OnClientConnected(EventArgs.Empty);
            });

            // Infinite read buffer loop
            while (true)
            {
                try
                {
                    // Based on the protocol we've defined, the first uint is the size of the message
                    uint readLength = await reader.LoadAsync(sizeof(uint));

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < sizeof(uint))
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    uint currentLength = reader.ReadUInt32();

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);

                    await rootPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        rootPage.Log("RFCOMM_Server::Received: " + message);
                        MessageReceivedEventArgs argument = new MessageReceivedEventArgs();
                        argument.Message = message;
                        OnMessageReceived(argument);
                    });
                }
                catch (Exception ex)
                {
                    switch ((uint)ex.HResult)
                    {
                        case (0x800703E3):
                            await rootPage.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                rootPage.Log("RFCOMM_Server::Client Disconnected Successfully");
                            });
                            break;
                        default:
                            throw;
                    }
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection)
            {
                isListening = false;
                Disconnect();
                rootPage.Log("RFCOMM_Server::Client disconnected.");
                OnClientDisonnected(EventArgs.Empty);
            }
        }

        public class MessageReceivedEventArgs : EventArgs
        {
            public string Message { get; set; }
        }
    }
}
