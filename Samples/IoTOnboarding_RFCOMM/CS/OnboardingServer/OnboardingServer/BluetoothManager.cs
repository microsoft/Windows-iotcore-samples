using System;
using System.Collections.ObjectModel;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Devices.Enumeration;
using DeviceEnumerationHelpers;
using Windows.Foundation;
using System.Threading.Tasks;

namespace OnboardingServer
{
    public partial class BluetoothManager
    {
        private bool pairSucceed = false;
        private MainPage rootPage;
        private DeviceWatcher deviceWatcher = null;
        private DispatcherTimer dispatcherTimer;

        //Event handlers
        private TypedEventHandler<DeviceWatcher, DeviceInformation> handlerAdded = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerUpdated = null;
        private TypedEventHandler<DeviceWatcher, DeviceInformationUpdate> handlerRemoved = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerEnumCompleted = null;
        private TypedEventHandler<DeviceWatcher, Object> handlerStopped = null;

        private ObservableCollection<DeviceInformationDisplay> ResultCollection = null;

        public BluetoothManager(MainPage mainPage)
        {
            ResultCollection = new ObservableCollection<DeviceInformationDisplay>();
            rootPage = mainPage;

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += DispatcherTimer_TickAsync;
            // Retry pairing to Manager Device every 3 seconds after the initial pairing fails.
            // TODO: Change the interval to your preferred timespan.
             dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 3);
        }

        public void StartWatcher()
        {
            ResultCollection.Clear();

            // Get the Bluettooth device selector
            DeviceSelectorInfo deviceSelectorInfo = DeviceSelectorChoices.Bluetooth;
            string selector = deviceSelectorInfo.Selector;
            deviceWatcher = DeviceInformation.CreateWatcher(
                    selector,
                    null, // don't request additional properties for this sample
                    deviceSelectorInfo.Kind);

            /***
             * Hook up handlers for the watcher events before starting the watcher
             ***/

            //"Added" Event Handler
            handlerAdded = new TypedEventHandler<DeviceWatcher, DeviceInformation>(async (watcher, deviceInfo) =>
            {
                // Since we are updating to a UI element, we update on the UI thread.
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    ResultCollection.Add(new DeviceInformationDisplay(deviceInfo));
                    if (TARGET_NAME == deviceInfo.Name)
                    {
                        await PairDevice(ResultCollection[ResultCollection.Count - 1]);
                    }
                    rootPage.Log(String.Format("BT_MANANGER::WATCHER_Added::STATUS: {0} added.", deviceInfo.Name));
                });
            });
            deviceWatcher.Added += handlerAdded;

            //"Updated" Event Handler
            handlerUpdated = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we are updating to a UI element, we update on the UI thread.
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, async () =>
                {
                    // Find the corresponding updated DeviceInformation in the collection and pass the update object
                    // to the Update method of the existing DeviceInformation. This automatically updates the object
                    // for us.
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            deviceInfoDisp.Update(deviceInfoUpdate);

                            rootPage.Log(String.Format("BT_MANANGER::WATCHER_Updated::STATUS: {0} updated.", deviceInfoDisp.Name));

                            if (TARGET_NAME == deviceInfoDisp.Name && !deviceInfoDisp.IsPaired)
                            {
                                await PairDevice(deviceInfoDisp);
                            }
                        }
                    }
                });
            });
            deviceWatcher.Updated += handlerUpdated;

            //"Removed" Event Handler
            handlerRemoved = new TypedEventHandler<DeviceWatcher, DeviceInformationUpdate>(async (watcher, deviceInfoUpdate) =>
            {
                // Since we are updating to a UI element, we need to update on the UI thread.
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    // Find the corresponding DeviceInformation in the collection and remove it
                    foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                    {
                        if (deviceInfoDisp.Id == deviceInfoUpdate.Id)
                        {
                            ResultCollection.Remove(deviceInfoDisp);
                            rootPage.Log(String.Format("BT_MANANGER::WATCHER_REMOVED::STATUS: {0} removed.", deviceInfoDisp.Name));
                            break;
                        }
                    }

                    rootPage.Log(String.Format("BT_MANANGER::WATCHER_REMOVED::STATUS: {0} devices found.", ResultCollection.Count));
                });
            });
            deviceWatcher.Removed += handlerRemoved;

            //"EnumerationCompleted" Event Handler
            handlerEnumCompleted = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    rootPage.Log(
                        String.Format("BT_MANANGER::WATCHER_ENUMDONE::STATUS: {0} devices found. Enumeration completed. Watching for updates...",
                        ResultCollection.Count));
                });
            });
            deviceWatcher.EnumerationCompleted += handlerEnumCompleted;

            //"Stopped" Event Handler
            handlerStopped = new TypedEventHandler<DeviceWatcher, Object>(async (watcher, obj) =>
            {
                await rootPage.Dispatcher.RunAsync(CoreDispatcherPriority.Low, () =>
                {
                    rootPage.Log(
                        String.Format("BT_MANANGER::WATCHER_STOPPED::STATUS: {0} devices found. Watcher {1}.",
                            ResultCollection.Count,
                            DeviceWatcherStatus.Aborted == watcher.Status ? "aborted" : "stopped"));
                });
            });
            deviceWatcher.Stopped += handlerStopped;

            //Start device watcher
            rootPage.Log("BT_MANANGER::WATCHER::STATUS: Starting Watcher...");
            deviceWatcher.Start();
        }

        public void StopWatcher()
        {
            if (dispatcherTimer.IsEnabled)
            {    
                dispatcherTimer.Stop();
            }

            if (null != deviceWatcher)
            {
                // First unhook all event handlers except the stopped handler. This ensures our
                // event handlers don't get called after stop, as stop won't block for any "in flight" 
                // event handler calls.  We leave the stopped handler as it's guaranteed to only be called
                // once and we'll use it to know when the query is completely stopped. 
                deviceWatcher.Added -= handlerAdded;
                deviceWatcher.Updated -= handlerUpdated;
                deviceWatcher.Removed -= handlerRemoved;
                deviceWatcher.EnumerationCompleted -= handlerEnumCompleted;

                if (DeviceWatcherStatus.Started == deviceWatcher.Status ||
                    DeviceWatcherStatus.EnumerationCompleted == deviceWatcher.Status)
                {
                    deviceWatcher.Stop();
                }
            }
        }

        private async Task PairDevice(DeviceInformationDisplay targetDevice)
        {
            pairSucceed = false;
            if (dispatcherTimer.IsEnabled)
            {
                dispatcherTimer.Stop();
            }

            // Gray out the pair button and results view while pairing is in progress.
            rootPage.Log(String.Format("BT_MANANGER::PairDevice::STATUS:  Pairing with {0} started. Please wait...", targetDevice.Name));

            DevicePairingResult dpr = await targetDevice.DeviceInformation.Pairing.PairAsync(DevicePairingProtectionLevel.None);

            pairSucceed = (dpr.Status == DevicePairingResultStatus.Paired) || (dpr.Status == DevicePairingResultStatus.AlreadyPaired);
            rootPage.Log(
               String.Format("BT_MANANGER::PairDevice::{0}: Pairing result = {1}", pairSucceed ? "STATUS" : "ERROR", dpr.Status.ToString()));

            if (!pairSucceed)
            {
                dispatcherTimer.Start();
            }
        }

        private void DispatcherTimer_TickAsync(object sender, object e)
        {
            if (!pairSucceed)
            {
                foreach (DeviceInformationDisplay deviceInfoDisp in ResultCollection)
                {
                    if (TARGET_NAME == deviceInfoDisp.Name)
                    {
                        PairDevice(deviceInfoDisp);
                    }
                }
            }
            else
            {
                dispatcherTimer.Stop();
            }
        }
    }
}
