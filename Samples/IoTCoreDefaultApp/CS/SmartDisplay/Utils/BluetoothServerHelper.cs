// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace SmartDisplay.Bluetooth
{
    public class BluetoothMessageReceivedArgs : EventArgs
    {
        public string Message;
    }

    public class BluetoothServerHelper : IDisposable
    {
        public delegate void BluetoothMessageReceivedHandler(object sender, BluetoothMessageReceivedArgs e);

        public event BluetoothMessageReceivedHandler MessageReceived;
        public event EventHandler ClientDisconnected;

        public static BluetoothServerHelper Instance { get; } = new BluetoothServerHelper();

        private StreamSocket _socket;
        private DataWriter _writer;
        private RfcommServiceProvider _rfcommProvider;
        private StreamSocketListener _socketListener;
        private bool _isInitialized = false;
        private Timer _btTimer;

        /// <summary>
        /// Timer for automatically connecting to BT devices that are hosting the communication service
        /// </summary>
        public void StartTimer()
        {
            _btTimer = new Timer(TimerCallback, null, 3000, Timeout.Infinite);
        }

        /// <summary>
        /// Callback for timer.  It will attempt to connect to the devices it finds with the service running and then try every 10 seconds until it does.
        /// </summary>
        /// <param name="state"></param>
        private async void TimerCallback(object state)
        {
            App.LogService.Write("Trying to connect to paired BT devices...");

            if (_socket == null)
            {
                var selector = RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(BluetoothConstants.RfcommServiceUuid));
                var deviceCollection = await DeviceInformation.FindAllAsync(selector);

                App.LogService.Write($"Found {deviceCollection.Count} devices");

                foreach (var info in deviceCollection)
                {
                    var service = await RfcommDeviceService.FromIdAsync(info.Id);

                    App.LogService.Write("Connecting to " + info.Name + "...");

                    try
                    {
                        if (service != null)
                        {
                            // Cancel if it takes more than 10 seconds to connect to the device
                            using (var cts = new CancellationTokenSource())
                            {
                                cts.CancelAfter(10000);

                                _socket = new StreamSocket();
                                var op = _socket.ConnectAsync(service.ConnectionHostName, service.ConnectionServiceName);
                                var connectTask = op.AsTask(cts.Token);

                                App.LogService.Write("[" + info.Name + "] Connecting to stream socket...");
                                await connectTask;

                                if (connectTask.IsCanceled)
                                {
                                    App.LogService.Write("[" + info.Name + "] Connect timed out.");
                                    _socket.Dispose();
                                    _socket = null;
                                }
                                else
                                {
                                    App.LogService.Write("Successfully connected to " + info.Name + ", starting data reader...");
                                    StartReader(_socket);
                                    return;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Couldn't connect to the device
                        switch (ex.HResult)
                        {
                            case -2147024637:   // No more data available
                            case -2147014845:   // A socket operation was attempted to an unreachable network
                            case -2146233029:   // The operation was cancelled
                                if (_socket != null)
                                {
                                    App.LogService.Write("[" + info.Name + "] Closing socket...", LoggingLevel.Error);
                                    _socket.Dispose();
                                    _socket = null;
                                }
                                break;
                            default:
                                throw;
                        }
                    }
                }
            }

            // Start timer and try again
            _btTimer = new Timer(TimerCallback, null, 10000, Timeout.Infinite);
        }

        /// <summary>
        /// Initializes the server using RfcommServiceProvider to advertise the Service UUID and start listening
        /// for incoming connections.
        /// </summary>
        public async Task<bool> InitializeRfcommServer()
        {
            App.LogService.Write("Initializing RFCOMM server...");

            if (_isInitialized)
            {
                App.LogService.Write("RFCOMM server already initialized!");
                return true;
            }

            try
            {
                _rfcommProvider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(BluetoothConstants.RfcommServiceUuid));
            }
            // Catch exception HRESULT_FROM_WIN32(ERROR_DEVICE_NOT_AVAILABLE).
            catch (Exception ex) when ((uint)ex.HResult == 0x800710DF)
            {
                // The Bluetooth radio may be off.
                App.LogService.Write("Make sure your Bluetooth Radio is on: " + ex.Message);
                return false;
            }

            // Create a listener for this service and start listening
            App.LogService.Write("Creating socket listener...");
            _socketListener = new StreamSocketListener();
            _socketListener.ConnectionReceived += OnConnectionReceived;
            var rfcomm = _rfcommProvider.ServiceId.AsString();

            await _socketListener.BindServiceNameAsync(_rfcommProvider.ServiceId.AsString(),
                SocketProtectionLevel.BluetoothEncryptionAllowNullAuthentication);

            // Set the SDP attributes and start Bluetooth advertising
            InitializeServiceSdpAttributes(_rfcommProvider);

            try
            {
                App.LogService.Write("Starting BT advertisement...");
                _rfcommProvider.StartAdvertising(_socketListener, true);
            }
            catch (Exception e)
            {
                // If you aren't able to get a reference to an RfcommServiceProvider, tell the user why.
                // Usually throws an exception if user changed their privacy settings to prevent Sync w/ Devices.  
                App.LogService.Write(e.Message, LoggingLevel.Error);
                return false;
            }

            App.LogService.Write("Listening for incoming connections");

            _isInitialized = true;

            return true;
        }

        /// <summary>
        /// Creates the SDP record that will be revealed to the Client device when pairing occurs.  
        /// </summary>
        /// <param name="rfcommProvider">The RfcommServiceProvider that is being used to initialize the server</param>
        private void InitializeServiceSdpAttributes(RfcommServiceProvider rfcommProvider)
        {
            App.LogService.Write("Initializing SDP attributes...");

            var sdpWriter = new DataWriter();

            // Write the Service Name Attribute.
            sdpWriter.WriteByte(BluetoothConstants.SdpServiceNameAttributeType);

            // The length of the UTF-8 encoded Service Name SDP Attribute.
            sdpWriter.WriteByte((byte)BluetoothConstants.SdpServiceName.Length);

            // The UTF-8 encoded Service Name value.
            sdpWriter.UnicodeEncoding = Windows.Storage.Streams.UnicodeEncoding.Utf8;
            sdpWriter.WriteString(BluetoothConstants.SdpServiceName);

            // Set the SDP Attribute on the RFCOMM Service Provider.
            rfcommProvider.SdpRawAttributes.Add(BluetoothConstants.SdpServiceNameAttributeId, sdpWriter.DetachBuffer());
        }

        /// <summary>
        /// Invoked when the socket listener accepts an incoming Bluetooth connection.
        /// </summary>
        /// <param name="sender">The socket listener that accepted the connection.</param>
        /// <param name="args">The connection accept parameters, which contain the connected socket.</param>
        private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            // Don't need the listener anymore
            _socketListener.Dispose();
            _socketListener = null;

            StartReader(args.Socket);

            App.LogService.Write("Incoming Bluetooth connection received: " + args.Socket.Information.RemoteServiceName);
        }

        /// <summary>
        /// Starts the data reader on the socket to start reading any incoming messages
        /// </summary>
        /// <param name="socket"></param>
        private async void StartReader(StreamSocket socket)
        {
            try
            {
                _socket = socket;
            }
            catch (Exception e)
            {
                App.LogService.Write(e.Message, Windows.Foundation.Diagnostics.LoggingLevel.Error);
                return;
            }

            // Note - this is the supported way to get a Bluetooth device from a given socket
            var remoteDevice = await BluetoothDevice.FromHostNameAsync(socket.Information.RemoteHostName);

            _writer = new DataWriter(socket.OutputStream);
            var reader = new DataReader(socket.InputStream);
            bool remoteDisconnection = false;

            NotifyMessageReceived($"{remoteDevice.Name} has connected");

            App.LogService.Write("Connected to Client: " + remoteDevice.Name);
            App.LogService.Write("Starting data reader...");

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

                    App.LogService.Write("Current Length: " + currentLength);

                    // Load the rest of the message since you already know the length of the data expected.  
                    readLength = await reader.LoadAsync(currentLength);

                    App.LogService.Write("Read Length: " + currentLength);

                    // Check if the size of the data is expected (otherwise the remote has already terminated the connection)
                    if (readLength < currentLength)
                    {
                        remoteDisconnection = true;
                        break;
                    }
                    string message = reader.ReadString(currentLength);

                    NotifyMessageReceived($"{remoteDevice.Name}:  {message}");

                    App.LogService.Write("Received: " + message);
                }
                // Catch exception HRESULT_FROM_WIN32(ERROR_OPERATION_ABORTED).
                catch (Exception ex) when ((uint)ex.HResult == 0x800703E3)
                {
                    App.LogService.Write("Client Disconnected Successfully");
                    break;
                }
            }

            reader.DetachStream();
            if (remoteDisconnection)
            {
                Disconnect();
                App.LogService.Write("Client disconnected");
            }
        }

        public void Disconnect()
        {
            if (_rfcommProvider != null)
            {
                _rfcommProvider.StopAdvertising();
                _rfcommProvider = null;
            }

            if (_socketListener != null)
            {
                _socketListener.Dispose();
                _socketListener = null;
            }

            if (_writer != null)
            {
                _writer.DetachStream();
                _writer = null;
            }

            if (_socket != null)
            {
                _socket.Dispose();
                _socket = null;
            }

            _isInitialized = false;

            NotifyClientDisconnected();
        }

        protected virtual void NotifyMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, new BluetoothMessageReceivedArgs()
            {
                Message = message
            });
        }

        protected virtual void NotifyClientDisconnected()
        {
            ClientDisconnected?.Invoke(this, EventArgs.Empty);
        }

        #region IDisposable
        bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    _btTimer.Dispose();
                    _socket.Dispose();
                    _writer.Dispose();
                    _socketListener.Dispose();
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~BluetoothServerHelper()
        {
            Dispose(false);
        }
        #endregion
    }

    public class BluetoothConstants
    {
        // The Bluetooth server's custom service Uuid
        public static readonly Guid RfcommServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

        // The value of the Service Name SDP attribute
        public const string SdpServiceName = "Bluetooth Rfcomm Chat Service";

        // public static readonly Guid RfcommServiceUuid = Guid.Parse("6663E8AD-05E7-4377-AD0F-0149F394D111");
        // public const string SdpServiceName = "Bluetooth Rfcomm Smart Display Service";

        // The Id of the Service Name SDP attribute
        public const UInt16 SdpServiceNameAttributeId = 0x100;

        // The SDP Type of the Service Name SDP attribute.
        // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
        //    -  the Attribute Type size in the least significant 3 bits,
        //    -  the SDP Attribute Type value in the most significant 5 bits.
        public const byte SdpServiceNameAttributeType = (4 << 3) | 5;
    }
}
