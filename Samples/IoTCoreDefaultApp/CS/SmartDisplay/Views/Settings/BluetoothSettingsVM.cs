// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Bluetooth;
using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth.Rfcomm;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Networking.Sockets;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;

namespace SmartDisplay.ViewModels.Settings
{
    public class BluetoothSettingsVM : BaseViewModel
    {
        #region UI properties and commands

        public bool BluetoothWatcherEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool BluetoothToggleIsOn
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (_isBluetoothDiscoverable && SetStoredProperty(value))
                {
                    if (value)
                    {
                        StartWatcherWithConfirmationAsync();
                    }
                    else
                    {
                        StopWatcherWithConfirmationAsync();
                    }
                };
            }
        }

        public string ConfirmationMessageText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            private set { SetStoredProperty(value); }
        }

        public double PanelWidth { get; } = Constants.SettingsWidth;
        public ObservableCollection<BluetoothDeviceInfo> BluetoothDeviceCollection { get; } = new ObservableCollection<BluetoothDeviceInfo>();

        private enum MessageType
        {
            YesNoMessage,
            OKMessage,
            InformationalMessage
        };

        // Device watcher
        private DeviceWatcher _deviceWatcher = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformation> _handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> _handlerRemoved = null;

        // Pairing controls and notifications
        private Button _inProgressPairButton = null;
        private FlyoutBase _savedPairButtonFlyout = null;
        private BluetoothDeviceInfo _deviceInfo = null;
        private DevicePairingRequestedEventArgs _pairingRequestedArgs = null;
        private Deferral _deferral = null;
        private RfcommServiceProvider _provider = null;
        private static bool _isBluetoothDiscoverable = false;
        private bool _isPairing = false;

        #endregion

        #region Localized UI strings

        private string BluetoothAttemptingToPairFormat { get; } = Common.GetLocalizedText("BluetoothAttemptingToPairFormat");
        private string BluetoothConfirmOnlyText { get; } = Common.GetLocalizedText("BluetoothConfirmOnlyFormat");
        private string BluetoothConfirmPINMatchText { get; } = Common.GetLocalizedText("BluetoothConfirmPinMatchFormat");
        private string BluetoothDeviceNameUnknownText { get; } = Common.GetLocalizedText("BluetoothDeviceNameUnknownText");
        private string BluetoothDisplayPINText { get; } = Common.GetLocalizedText("BluetoothDisplayPinFormat");
        private string BluetoothInboundRegistrationFailed { get; } = Common.GetLocalizedText("BluetoothInboundRegistrationFailed");
        private string BluetoothInboundRegistrationSucceededFormat { get; } = Common.GetLocalizedText("BluetoothInboundRegistrationSucceededFormat");
        private string BluetoothIncorrectEnteredPIN { get; } = Common.GetLocalizedText("BluetoothIncorrectEnteredPIN");
        private string BluetoothNoDeviceAvailableFormat { get; } = Common.GetLocalizedText("BluetoothNoDeviceAvailableFormat");
        private string BluetoothPairingFailureFormat { get; } = Common.GetLocalizedText("BluetoothPairingFailureFormat");
        private string BluetoothPairingSuccessFormat { get; } = Common.GetLocalizedText("BluetoothPairingSuccessFormat");
        private string BluetoothPreferencesText { get; } = Common.GetLocalizedText("BluetoothPreferences/Text");
        private string BluetoothStartedWatching { get; } = Common.GetLocalizedText("BluetoothStartedWatching");
        private string BluetoothStoppedWatching { get; } = Common.GetLocalizedText("BluetoothStoppedWatching");
        private string BluetoothUnpairingFailureFormat { get; } = Common.GetLocalizedText("BluetoothUnpairingFailureFormat");
        private string BluetoothUnpairingSuccessFormat { get; } = Common.GetLocalizedText("BluetoothUnpairingSuccessFormat");
        private string NoButtonText { get; } = Common.GetLocalizedText("NoButtonText");
        private string OkButtonText { get; } = Common.GetLocalizedText("OkButtonText");
        private string YesButtonText { get; } = Common.GetLocalizedText("YesButtonText");

        #endregion

        #region Data containers, dispatcher, and services

        private ApplicationDataContainer LocalSettings => ApplicationData.Current?.LocalSettings;
        private ILogService LogService => AppService?.LogService;
        private ITelemetryService TelemetryService => AppService?.TelemetryService;

        #endregion

        public BluetoothSettingsVM() : base()
        {
            BluetoothToggleIsOn = false;
        }

        public void SetUpVM()
        {
            // Handle inbound pairing requests
            App.InboundPairingRequested += App_InboundPairingRequestedAsync;

            if (BluetoothWatcherEnabled)
            {
                if (_deviceWatcher == null || (_deviceWatcher.Status == DeviceWatcherStatus.Stopped))
                {
                    StartWatcherAsync();
                }
            }
        }

        public void TearDownVM()
        {
            App.InboundPairingRequested -= App_InboundPairingRequestedAsync;

            if (_deviceWatcher != null && _deviceWatcher.Status != DeviceWatcherStatus.Stopped)
            {
                StopWatcher();
            }
        }

        public void SetUpBluetooth()
        {
            _inProgressPairButton = new Button();
            _savedPairButtonFlyout = _inProgressPairButton.Flyout;
            RegisterForInboundPairingRequests();
        }

        /// <summary>
        /// Make this device discoverable to other Bluetooth devices within range
        /// </summary>
        /// <returns></returns>
        private async Task<bool> MakeDiscoverable()
        {
            // Make the system discoverable. Don't repeatedly do this or the StartAdvertising will throw "cannot create a file when that file already exists."
            if (!_isBluetoothDiscoverable)
            {
                try
                {
                    Guid BluetoothServiceUuid = new Guid(Constants.BluetoothServiceUuid);
                    _provider = await RfcommServiceProvider.CreateAsync(RfcommServiceId.FromUuid(BluetoothServiceUuid));
                    StreamSocketListener listener = new StreamSocketListener();
                    listener.ConnectionReceived += OnConnectionReceived;
                    await listener.BindServiceNameAsync(_provider.ServiceId.AsString(), SocketProtectionLevel.PlainSocket);

                    // No need to set SPD attributes
                    _provider.StartAdvertising(listener, true);
                    _isBluetoothDiscoverable = true;
                }
                catch (Exception ex)
                {
                    string confirmationMessage = string.Format(BluetoothNoDeviceAvailableFormat, ex.Message);
                    await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);
                }
            }

            return _isBluetoothDiscoverable;
        }

        /// <summary>
        /// Register the device for inbound pairing requests
        /// </summary>
        private async void RegisterForInboundPairingRequests()
        {
            // Make the system discoverable for Bluetooth.
            await MakeDiscoverable();
            string confirmationMessage = string.Empty;

            // If the attempt to make the system discoverable failed then likely there is no Bluetooth device present,
            // so leave the diagnostic message put up by the call to MakeDiscoverable().
            if (_isBluetoothDiscoverable)
            {
                // Get state of ceremony checkboxes
                DevicePairingKinds ceremoniesSelected = GetSelectedCeremonies();
                int iCurrentSelectedCeremonies = (int)ceremoniesSelected;
                int iSavedSelectedCeremonies = -1; // Deliberate impossible value

                // Find out if we changed the ceremonies we originally registered with.
                // If we have registered before, these will be saved.
                object supportedPairingKinds = LocalSettings.Values["supportedPairingKinds"];

                if (supportedPairingKinds != null)
                {
                    iSavedSelectedCeremonies = (int)supportedPairingKinds;
                }

                if (!DeviceInformationPairing.TryRegisterForAllInboundPairingRequests(ceremoniesSelected))
                {
                    confirmationMessage = string.Format(BluetoothInboundRegistrationFailed, ceremoniesSelected.ToString());
                }
                else
                {
                    // Save off the ceremonies we registered with
                    LocalSettings.Values["supportedPairingKinds"] = iCurrentSelectedCeremonies;
                    confirmationMessage = string.Format(BluetoothInboundRegistrationSucceededFormat, ceremoniesSelected.ToString());
                }

                BluetoothWatcherEnabled = true;
            }
            else
            {
                BluetoothWatcherEnabled = false;
            }

            await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);
        }

        /// <summary>
        /// Called when an inbound Bluetooth connection is requested
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="inboundArgs"></param>
        private async void App_InboundPairingRequestedAsync(object sender, InboundPairingEventArgs inboundArgs)
        {
            LogService.Write("Bluetooth inbound pairing requested", LoggingLevel.Information);
            BluetoothServerHelper.Instance.Disconnect();

            // Ignore the inbound if pairing is already in progress
            if (_inProgressPairButton == null && !_isPairing)
            {
                try
                {
                    _isPairing = true;

                    // Restore the ceremonies we registered with
                    LogService.Write("Restoring supported ceremonies...", LoggingLevel.Information);

                    Object supportedPairingKinds = LocalSettings.Values["supportedPairingKinds"];
                    int iSelectedCeremonies = (int)DevicePairingKinds.ConfirmOnly;

                    if (supportedPairingKinds != null)
                    {
                        iSelectedCeremonies = (int)supportedPairingKinds;
                    }

                    // Clear any previous devices
                    LogService.Write("Refreshing Bluetooth devices...", LoggingLevel.Information);
                    BluetoothDeviceCollection.Clear();

                    // Add the latest information to display
                    BluetoothDeviceInfo currentDevice = new BluetoothDeviceInfo(inboundArgs.DeviceInfo);
                    BluetoothDeviceCollection.Add(currentDevice);

                    // Display a message about the inbound request
                    string confirmationMessage = string.Format(BluetoothAttemptingToPairFormat, currentDevice.Name, currentDevice.Id);

                    // Get the ceremony type and protection level selections
                    DevicePairingKinds ceremoniesSelected = GetSelectedCeremonies();

                    // Get the protection level
                    DevicePairingProtectionLevel protectionLevel = currentDevice.DeviceInformation.Pairing.ProtectionLevel;

                    // Specify custom pairing with all ceremony types and protection level EncryptionAndAuthentication
                    DeviceInformationCustomPairing customPairing = currentDevice.DeviceInformation.Pairing.Custom;
                    customPairing.PairingRequested += PairingRequestedHandlerAsync;
                    DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);

                    if (result.Status == DevicePairingResultStatus.Paired)
                    {
                        confirmationMessage = string.Format(BluetoothPairingSuccessFormat, currentDevice.Name, currentDevice.IdWithoutProtocolPrefix);
                    }
                    else
                    {
                        confirmationMessage = string.Format(BluetoothPairingFailureFormat, currentDevice.Name, currentDevice.IdWithoutProtocolPrefix);
                    }

                    // Display the result of the pairing attempt
                    await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);
                }
                catch (Exception ex)
                {
                    LogService.Write(ex.ToString(), LoggingLevel.Error);
                }

                _isPairing = false;
            }
            else
            {
                LogService.Write("Pairing already in progress", LoggingLevel.Information);
            }
        }

        /// <summary>
        /// Clear listed devices, start a Bluetooth watcher, and display a notification message
        /// </summary>
        public async void StartWatcherWithConfirmationAsync()
        {
            BluetoothDeviceCollection.Clear();
            StartWatcherAsync();
            await DisplayMessagePanel(BluetoothStartedWatching, MessageType.InformationalMessage);
        }

        /// <summary>
        /// Clear listed devices, stop a Bluetooth watcher, and display a notification message
        /// </summary>
        public async void StopWatcherWithConfirmationAsync()
        {
            BluetoothDeviceCollection.Clear();
            StopWatcher();
            await DisplayMessagePanel(BluetoothStoppedWatching, MessageType.InformationalMessage);
        }

        /// <summary>
        /// Method to start the Bluetooth watcher
        /// </summary>
        /// <returns></returns>
        private async void StartWatcherAsync()
        {
            await Task.Run(() =>
            {
                // Bluetooth + BluetoothLE
                string aqsFilter = "System.Devices.Aep.ProtocolId:=\"{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}\" OR " +
                                   "System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\"";

                // Request the IsPaired property so we can display the paired status in the UI
                string[] requestedProperties = { "System.Devices.Aep.IsPaired" };

                // Get the device selector chosen by the UI, then 'AND' it with the 'CanPair' property
                _deviceWatcher = DeviceInformation.CreateWatcher(
                    aqsFilter,
                    requestedProperties,
                    DeviceInformationKind.AssociationEndpoint);

                // Hook up handlers for the watcher events before starting the watcher. 
                // An EnumerationCompleted and Stopped handler are not shown here, but also available to use.
                _handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>((watcher, currentDevice) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    InvokeOnUIThread(() =>
                    {
                        if (currentDevice.Pairing.CanPair || currentDevice.Pairing.IsPaired)
                        {
                            BluetoothDeviceCollection.Add(new BluetoothDeviceInfo(currentDevice));
                        }
                    });
                });
                _deviceWatcher.Added += _handlerAdded;

                _handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    InvokeOnUIThread(() =>
                    {
                        // Find the corresponding updated DeviceInformation in the collection and pass the update object
                        // to the Update method of the existing DeviceInformation. This automatically updates the object
                        // for us.
                        foreach (BluetoothDeviceInfo currentDevice in BluetoothDeviceCollection)
                        {
                            if (currentDevice.Id == deviceInfoUpdate.Id)
                            {
                                currentDevice.Update(deviceInfoUpdate);
                                break;
                            }
                        }
                    });
                });
                _deviceWatcher.Updated += _handlerUpdated;

                _handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>((watcher, deviceInfoUpdate) =>
                {
                    // Since we have the collection databound to a UI element, we need to update the collection on the UI thread.
                    InvokeOnUIThread(() =>
                    {
                        // Find the corresponding DeviceInformation in the collection and remove it
                        foreach (BluetoothDeviceInfo currentDevice in BluetoothDeviceCollection)
                        {
                            if (currentDevice.Id == deviceInfoUpdate.Id)
                            {
                                BluetoothDeviceCollection.Remove(currentDevice);
                                break;
                            }
                        }
                    });
                });
                _deviceWatcher.Removed += _handlerRemoved;

                // Start the Device Watcher
                _deviceWatcher.Start();
            });
        }

        /// <summary>
        /// Method to stop the Bluetooth watcher 
        /// </summary>
        private void StopWatcher()
        {
            if (null != _deviceWatcher)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                _deviceWatcher.Added -= _handlerAdded;
                _deviceWatcher.Updated -= _handlerUpdated;
                _deviceWatcher.Removed -= _handlerRemoved;

                if (_deviceWatcher.Status != DeviceWatcherStatus.Stopped)
                {
                    _deviceWatcher.Stop();
                }
            }
        }

        /// <summary>
        /// Accept the current pairing after confirming the user entered the correct device PIN
        /// </summary>
        /// <param name="inputPIN"></param>
        public void AcceptPairingUsingInputPIN(string inputPIN)
        {
            // If the PIN the user enters does not match the device PIN, return
            if (inputPIN != _pairingRequestedArgs.Pin)
            {
                ConfirmationMessageText = BluetoothIncorrectEnteredPIN;
                return;
            }

            // Close the flyout 
            _inProgressPairButton.Flyout.Hide();
            _inProgressPairButton.Flyout = null;

            // Use the PIN to accept the pairing
            AcceptPairingWithPIN(inputPIN);
        }

        /// <summary>
        /// Accept the current pairing using the PIN parameter and complete the deferral
        /// </summary>
        /// <param name="PIN"></param>
        private void AcceptPairingWithPIN(string PIN)
        {
            if (_pairingRequestedArgs != null)
            {
                _pairingRequestedArgs.Accept(PIN);
                _pairingRequestedArgs = null;
            }

            CompleteDeferral();
        }

        /// <summary>
        /// Called when a custom pairing is initiated so that we can handle its custom ceremony
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        public async void PairingRequestedHandlerAsync(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            LogService.Write((Enum.GetName(typeof(DevicePairingKinds), args.PairingKind)), LoggingLevel.Information);

            BluetoothDeviceInfo currentDevice = new BluetoothDeviceInfo(args.DeviceInformation);

            // Save the args for use in ProvidePin case
            _pairingRequestedArgs = args;

            // Save the deferral away and complete it where necessary.
            if (args.PairingKind != DevicePairingKinds.DisplayPin)
            {
                _deferral = args.GetDeferral();
            }

            switch (args.PairingKind)
            {
                // Windows itself will pop the confirmation dialog as part of "consent" depending on which operating OS is running
                case DevicePairingKinds.ConfirmOnly:
                    {
                        var confirmationMessage = string.Format(BluetoothConfirmOnlyText, args.DeviceInformation.Name, args.DeviceInformation.Id);
                        if (await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage))
                        {
                            AcceptPairing();
                        }
                    }
                    break;
                // We only show the PIN on this side. The ceremony is actually completed when the user enters the PIN on the target device.
                case DevicePairingKinds.DisplayPin:
                    {
                        var confirmationMessage = string.Format(BluetoothDisplayPINText, args.Pin);
                        await DisplayMessagePanel(confirmationMessage, MessageType.OKMessage);
                    }
                    break;
                // A PIN may be shown on the target device and the user needs to enter the matching PIN on the originating device.
                case DevicePairingKinds.ProvidePin:
                    {
                        _inProgressPairButton.Flyout = _savedPairButtonFlyout;
                        _inProgressPairButton.Flyout.ShowAt(_inProgressPairButton);
                    }
                    break;
                // We show the PIN here and the user responds with whether the PIN matches what is displayed on the target device.
                case DevicePairingKinds.ConfirmPinMatch:
                    {
                        var confirmationMessage = string.Format(BluetoothConfirmPINMatchText, args.Pin);
                        if (await DisplayMessagePanel(confirmationMessage, MessageType.YesNoMessage))
                        {
                            AcceptPairing();
                        }
                    }
                    break;
            }
        }

        /// <summary>
        /// Accept the pairing and complete the deferral
        /// </summary>
        public void AcceptPairing()
        {
            LogService.Write("Bluetooth Pairing Accepted", LoggingLevel.Information);
            TelemetryService.WriteEvent("BluetoothDevicePaired");

            if (_pairingRequestedArgs != null)
            {
                _pairingRequestedArgs.Accept();
                _pairingRequestedArgs = null;
            }

            CompleteDeferral();
        }

        /// <summary>
        /// Complete the deferral
        /// </summary>
        public void CompleteDeferral()
        {
            _deferral?.Complete();
            _deferral = null;
        }

        /// <summary>
        /// Called to pair a targeted device
        /// </summary>
        /// <param name="pairButton">Pair button</param>
        /// <param name="currentDevice">Displayable information for the targeted device</param>
        public async Task PairingRequestedAsync(Button pairButton, BluetoothDeviceInfo currentDevice)
        {
            try
            {
                _deviceInfo = currentDevice;
                _inProgressPairButton = pairButton;

                // Display confirmation message panel
                string deviceIdentifier = _deviceInfo.Name != BluetoothDeviceNameUnknownText ? _deviceInfo.Name : _deviceInfo.IdWithoutProtocolPrefix;
                string confirmationMessage = string.Format(BluetoothAttemptingToPairFormat, _deviceInfo.Name, _deviceInfo.IdWithoutProtocolPrefix);

                await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);

                // Save the flyout and set to null so it doesn't appear without explicitly being called 
                _savedPairButtonFlyout = pairButton.Flyout;
                _inProgressPairButton.Flyout = null;
                pairButton.IsEnabled = false;

                // Specify custom pairing with all ceremony types and protection level EncryptionAndAuthentication
                DevicePairingKinds ceremoniesSelected = GetSelectedCeremonies();
                DevicePairingProtectionLevel protectionLevel = DevicePairingProtectionLevel.Default;

                // Setup a custom pairing and handler, then get the results of the request
                DeviceInformationCustomPairing customPairing = _deviceInfo.DeviceInformation.Pairing.Custom;
                customPairing.PairingRequested += PairingRequestedHandlerAsync;
                DevicePairingResult result = await customPairing.PairAsync(ceremoniesSelected, protectionLevel);

                if (result.Status == DevicePairingResultStatus.Paired)
                {
                    confirmationMessage = string.Format(BluetoothPairingSuccessFormat, deviceIdentifier, result.Status.ToString());
                }
                else
                {
                    confirmationMessage = string.Format(BluetoothPairingFailureFormat, deviceIdentifier, result.Status.ToString());
                }

                // Display the result of the pairing attempt
                await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);

                // If the watcher toggle is on, clear any devices in the list and stop and restart the watcher to ensure their current state is reflected
                if (BluetoothWatcherEnabled)
                {
                    BluetoothDeviceCollection.Clear();
                    StopWatcher();
                    StartWatcherAsync();
                }
                else
                {
                    // If the watcher is off, this is an inbound request so we only need to clear the list
                    BluetoothDeviceCollection.Clear();
                }

                _inProgressPairButton = null;
                pairButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Called to unpair a targeted device 
        /// </summary>
        /// <param name="unpairButton">Unpair button</param>
        /// <param name="currentDevice">Displayable information for the targeted device</param>
        public async Task UnpairDeviceAsync(Button unpairButton, BluetoothDeviceInfo currentDevice)
        {
            try
            {
                string confirmationMessage = string.Empty;

                // Disable the unpair button until we are done
                unpairButton.IsEnabled = false;
                DeviceUnpairingResult unpairingResult = await currentDevice.DeviceInformation.Pairing.UnpairAsync();

                if (unpairingResult.Status == DeviceUnpairingResultStatus.Unpaired)
                {
                    // Device is unpaired
                    confirmationMessage = string.Format(BluetoothUnpairingSuccessFormat, currentDevice.Name, currentDevice.IdWithoutProtocolPrefix);
                }
                else
                {
                    confirmationMessage = string.Format(BluetoothUnpairingFailureFormat,
                        unpairingResult.Status.ToString(),
                        currentDevice.Name,
                        currentDevice.IdWithoutProtocolPrefix);
                }

                // Display the result of the unpairing attempt
                await DisplayMessagePanel(confirmationMessage, MessageType.InformationalMessage);

                // If the watcher toggle is on, clear any devices in the list and stop and restart the watcher to ensure state is reflected in list
                if (BluetoothWatcherEnabled)
                {
                    BluetoothDeviceCollection.Clear();
                    StopWatcher();
                    StartWatcherAsync();
                }
                else
                {
                    // If the watcher is off this is an inbound request so just clear the list
                    BluetoothDeviceCollection.Clear();
                }

                // Enable the unpair button
                unpairButton.IsEnabled = true;
            }
            catch (Exception ex)
            {
                LogService.Write(ex.ToString(), LoggingLevel.Error);
            }
        }

        /// <summary>
        /// Get the set of acceptable ceremonies from the check boxes
        /// </summary>
        /// <returns>Types of I/O Bluetooth connections supported by the device</returns>
        private DevicePairingKinds GetSelectedCeremonies()
        {
            DevicePairingKinds ceremonySelection = DevicePairingKinds.ConfirmOnly
                                                   | DevicePairingKinds.DisplayPin
                                                   | DevicePairingKinds.ProvidePin
                                                   | DevicePairingKinds.ConfirmPinMatch;
            return ceremonySelection;
        }

        /// <summary>
        /// Used to display different types of notification dialogs based on the message
        /// </summary>
        /// <param name="confirmationMessage">The message to display as a string</param>
        /// <param name="messageType">MessageType of the message</param>
        private async Task<bool> DisplayMessagePanel(string confirmationMessage, MessageType messageType)
        {
            ConfirmationMessageText = confirmationMessage;

            switch (messageType)
            {
                // For notification messages that don't require user interaction
                case MessageType.InformationalMessage:
                    return true;

                // For confirmation messages that only require user acknowledgement
                case MessageType.OKMessage:
                    return await AppService.YesNoAsync(BluetoothPreferencesText, confirmationMessage, OkButtonText);

                // For confirmation messages that prompt the user to make a yes or no selection 
                case MessageType.YesNoMessage:
                    return await AppService.YesNoAsync(BluetoothPreferencesText, confirmationMessage, YesButtonText, NoButtonText);

                default:
                    return false;
            }
        }

        /// <summary>
        /// We have to have a callback handler to handle "ConnectionReceived" but we don't do anything because
        /// the StartAdvertising is just a way to turn on Bluetooth discoverability
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void OnConnectionReceived(StreamSocketListener sender, StreamSocketListenerConnectionReceivedEventArgs args)
        {
            return;
        }
    }
}
