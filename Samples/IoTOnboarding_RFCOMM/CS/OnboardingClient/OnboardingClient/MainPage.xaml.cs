using System;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;


namespace OnboardingClient
{
    public sealed partial class MainPage : Page
    {
        private char[] separator = new char[] { ';' };
        private StreamSocket chatSocket = null;
        private DataWriter chatWriter = null;
        private RfcommDeviceService chatService = null;
        private DeviceInformationCollection chatServiceDeviceCollection = null;

        public MainPage()
        {
            this.InitializeComponent();
            App.Current.Suspending += App_Suspending;
            ConversationListBox.Items.VectorChanged += ConversationListScrollBottom;
            NotifyUser("Click \"Start\" to begin.", NotifyType.StatusMessage);
        }

        private void ConversationListScrollBottom(Windows.Foundation.Collections.IObservableVector<object> sender, Windows.Foundation.Collections.IVectorChangedEventArgs @event)
        {
            if (ConversationListBox.Items.Count > 0)
            {
                ConversationListBox.ScrollIntoView(ConversationListBox.Items[ConversationListBox.Items.Count-1]);
            }
        }

        void App_Suspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Make sure we clean up resources on suspend.
            Disconnect("App Suspension disconnects");
        }

        /// <summary>
        /// When the user presses the "Start" button, check to see if any of the currently paired devices support the Rfcomm chat service and display them in a list.  
        /// Note that in this case, the other device must be running the Rfcomm Chat Server before being paired.  
        /// </summary>
        /// <param name="sender">Instance that triggered the event.</param>
        /// <param name="e">Event data describing the conditions that led to the event.</param>
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            StartButton.IsEnabled = false;

            // Find all paired instances of the Rfcomm chat service and display them in a list
            chatServiceDeviceCollection = await DeviceInformation.FindAllAsync(
                RfcommDeviceService.GetDeviceSelector(RfcommServiceId.FromUuid(Constants.RfcommChatServiceUuid)));

            if (chatServiceDeviceCollection.Count > 0)
            {
                DeviceList.Items.Clear();
                foreach (var chatServiceDevice in chatServiceDeviceCollection)
                {
                    DeviceList.Items.Add(chatServiceDevice.Name);
                }
                NotifyUser("Select a device.", NotifyType.StatusMessage);
            }
            else
            {
                NotifyUser(
                    "No chat services were found. Please pair with a device that is advertising the chat service.",
                    NotifyType.ErrorMessage);
            }
            StartButton.IsEnabled = true;
        }

        /// <summary>
        /// Invoked once the user has selected the device to connect to.  
        /// Once the user has selected the device, we will automatically attempt to connect to the 
        /// chat service of the selected device. If the connection succeeds, we will automatically stream 
        /// the messages from the connected deviced.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void DeviceList_Tapped(object sender, TappedRoutedEventArgs e)
        {
            StartButton.IsEnabled = false;

            //Get chat service from the selected device.
            var chatServiceDevice = chatServiceDeviceCollection[DeviceList.SelectedIndex];
            chatService = await RfcommDeviceService.FromIdAsync(chatServiceDevice.Id);

            NotifyUser("Connecting ...", NotifyType.StatusMessage);

            if (chatService == null)
            {
                NotifyUser(
                    "Access to the device is denied because the application was not granted access",
                    NotifyType.StatusMessage);
                return;
            }

            // Do various checks of the SDP record to make sure you are talking to a device that actually supports the Bluetooth Rfcomm Chat Service 
            var attributes = await chatService.GetSdpRawAttributesAsync();
            if (!attributes.ContainsKey(Constants.SdpServiceNameAttributeId))
            {
                NotifyUser(
                    "The Chat service is not advertising the Service Name attribute (attribute id=0x100). " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                StartButton.IsEnabled = true;
                return;
            }

            var attributeReader = DataReader.FromBuffer(attributes[Constants.SdpServiceNameAttributeId]);
            var attributeType = attributeReader.ReadByte();
            if (attributeType != Constants.SdpServiceNameAttributeType)
            {
                NotifyUser(
                    "The Chat service is using an unexpected format for the Service Name attribute. " +
                    "Please verify that you are running the BluetoothRfcommChat server.",
                    NotifyType.ErrorMessage);
                StartButton.IsEnabled = true;
                return;
            }

            var serviceNameLength = attributeReader.ReadByte();

            // The Service Name attribute requires UTF-8 encoding.
            attributeReader.UnicodeEncoding = UnicodeEncoding.Utf8;
            string ServiceName = attributeReader.ReadString(serviceNameLength);

            lock (this)
            {
                chatSocket = new StreamSocket();
            }
            try
            {
                await chatSocket.ConnectAsync(chatService.ConnectionHostName, chatService.ConnectionServiceName);

                chatWriter = new DataWriter(chatSocket.OutputStream);
                DataReader chatReader = new DataReader(chatSocket.InputStream);
                NotifyUser("Connected to chat service \""+ ServiceName +"\".", NotifyType.StatusMessage);
                ConversationListBox.Items.Add("========== Session Connected ==========");
                DisconnectButton.IsEnabled = true;
                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                switch ((uint)ex.HResult)
                {
                    case (0x80070490): // ERROR_ELEMENT_NOT_FOUND
                    default:
                        NotifyUser("Please verify that " + chatServiceDevice.Name + "is running the BluetoothRfcommChat server.", NotifyType.ErrorMessage);
                        StartButton.IsEnabled = true;
                        break;
                }
            }
        }

        /// <summary>
        /// EventHandler for listening for the "Enter" key to send WiFi connection 
        /// information to the IoT device.
        /// </summary>
        public void PwdBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                string message = "CONNECT";
                message += separator[0];
                message += NetworkListComboBox.SelectedValue;
                message += separator[0];
                message += PwdInput.Password;
                SendMessage(message);
            }
        }

        /// <summary>
        /// EventHandler for "Connect" button click to send WiFi connection 
        /// information to the IoT device.
        /// </summary>
        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            string message = "CONNECT";
            message += separator[0];
            message += NetworkListComboBox.SelectedValue;
            message += separator[0];
            message += PwdInput.Password;
            SendMessage(message);
        }

        /// <summary>
        /// Takes the contents of the MessageTextBox and writes it to the outgoing chatWriter
        /// </summary>
        private async void SendMessage(string message)
        {
            try
            {
                if (message.Length != 0)
                {
                    chatWriter.WriteUInt32((uint)message.Length);
                    chatWriter.WriteString(message);

                    ConversationListBox.Items.Add("Sent: " + message);
                    message = "";
                    await chatWriter.StoreAsync();

                }
            }
            catch (Exception ex)
            {
                // TODO: Catch disconnect -  HResult = 0x80072745 - catch this (remote device disconnect) ex = {"An established connection was aborted by the software in your host machine. (Exception from HRESULT: 0x80072745)"}
                NotifyUser("Error: " + ex.HResult.ToString() + " - " + ex.Message,
                    NotifyType.StatusMessage);
            }
        }

        private void OnMessageReceived(string message)
        {
            RefreshButton.IsEnabled = false;
            string[] parsedMsg = message.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            if ("NETWORKLIST" == parsedMsg[0])
            {
                NetworkListComboBox.IsEnabled = false;

                NetworkListComboBox.Items.Clear();
                for (int i = 1; i < parsedMsg.Length; i++)
                {
                    NetworkListComboBox.Items.Add(parsedMsg[i]);
                }
                NetworkListComboBox.IsEnabled = true;
                ConnectButton.IsEnabled = true;
                RefreshButton.IsEnabled = true;
            }
        }

        private async void ReceiveStringLoop(DataReader chatReader)
        {
            try
            {
                uint size = await chatReader.LoadAsync(sizeof(uint));
                if (size < sizeof(uint))
                {
                    Disconnect("Remote device terminated connection");
                    return;
                }

                uint stringLength = chatReader.ReadUInt32();
                uint actualStringLength = await chatReader.LoadAsync(stringLength);
                if (actualStringLength != stringLength)
                {
                    // The underlying socket was closed before we were able to read the whole data
                    return;
                }

                string message = chatReader.ReadString(stringLength);
                ConversationListBox.Items.Add("Received: " + message);
                OnMessageReceived(message);

                ReceiveStringLoop(chatReader);
            }
            catch (Exception ex)
            {
                lock (this)
                {
                    if (chatSocket == null)
                    {
                        // Do not print anything here -  the user closed the socket.
                        // HResult = 0x80072745 - catch this (remote device disconnect) ex = {"An established connection was aborted by the software in your host machine. (Exception from HRESULT: 0x80072745)"}
                    }
                    else
                    {
                        Disconnect("Read stream failed with error: " + ex.Message);
                    }
                }
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            SendMessage("REFRESH;");
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ConversationListBox.Items.Clear();
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            Disconnect("Disconnected");
        }

        /// <summary>
        /// Cleans up the socket and DataWriter and reset the UI
        /// </summary>
        /// <param name="disconnectReason"></param>
        private void Disconnect(string disconnectReason)
        {
            if (chatWriter != null)
            {
                chatWriter.DetachStream();
                chatWriter = null;
            }

            if (chatService != null)
            {
                chatService.Dispose();
                chatService = null;
            }
            lock (this)
            {
                if (chatSocket != null)
                {
                    chatSocket.Dispose();
                    chatSocket = null;
                }
            }

            ConversationListBox.Items.Add("========== Session Disconnected ==========");
            NotifyUser(disconnectReason, NotifyType.StatusMessage);
            StartButton.IsEnabled = true;
            DisconnectButton.IsEnabled = false;
            NetworkListComboBox.IsEnabled = false;
            ConnectButton.IsEnabled = false;
            RefreshButton.IsEnabled = false;
        }

        /// <summary>
        /// Used to display messages to the user
        /// </summary>
        /// <param name="strMessage"></param>
        /// <param name="type"></param>
        public void NotifyUser(string strMessage, NotifyType type)
        {
            switch (type)
            {
                case NotifyType.StatusMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Green);
                    break;
                case NotifyType.ErrorMessage:
                    StatusBorder.Background = new SolidColorBrush(Windows.UI.Colors.Red);
                    break;
            }
            StatusBlock.Text = strMessage;
        }

        public enum NotifyType
        {
            StatusMessage,
            ErrorMessage
        };

        class Constants
        {
            // The Chat Server's custom service Uuid: 34B1CF4D-1069-4AD6-89B6-E161D79BE4D8
            public static readonly Guid RfcommChatServiceUuid = Guid.Parse("34B1CF4D-1069-4AD6-89B6-E161D79BE4D8");

            // The Id of the Service Name SDP attribute
            public const UInt16 SdpServiceNameAttributeId = 0x100;

            // The SDP Type of the Service Name SDP attribute.
            // The first byte in the SDP Attribute encodes the SDP Attribute Type as follows :
            //    -  the Attribute Type size in the least significant 3 bits,
            //    -  the SDP Attribute Type value in the most significant 5 bits.
            public const byte SdpServiceNameAttributeType = (4 << 3) | 5;
        }
    }
}
