using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using SystemUpdate.ViewModel;
using Windows.Foundation.Metadata;
using Windows.System.Update;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SystemUpdate
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        CoreDispatcher _dispatcher;
        ObservableCollection<UpdateItemViewModel> _items = new ObservableCollection<UpdateItemViewModel>();

        /// <summary>
        /// MainPage constructor
        /// </summary>
        public MainPage()
        {
            this.InitializeComponent();
            _dispatcher = Window.Current.Dispatcher;

            if (!ApiInformation.IsApiContractPresent("Windows.System.SystemManagementContract", 6, 0))
            {
                // SystemUpdateManager was first implemented in SystemManagementContract 6.0
                VisualStateManager.GoToState(this, "NotSupported", false);
                UpdateStateTextBlock.Text = "Windows.System.SystemManagementContract 6.0 not found";
            }
            else if (!SystemUpdateManager.IsSupported())
            {
                // The API must be supported by the current edition of Windows
                // This can also return false if the application doesn't have the systemManagement capability
                VisualStateManager.GoToState(this, "NotSupported", false);
                UpdateStateTextBlock.Text = "System Update not supported (or systemManagement capability missing)";
            }
            else
            {
                // Register for state change notifications
                SystemUpdateManager.StateChanged += SystemUpdateManager_StateChanged;

                // Display update information
                UpdateStateTextBlock.Text = GetResourceString(SystemUpdateManager.State.ToString());
                LastChecked.Text = SystemUpdateManager.LastUpdateCheckTime.ToString("G");
                LastInstalled.Text = SystemUpdateManager.LastUpdateInstallTime.ToString("G");

                // Attach ViewModel to ListView
                UpdateItemsListView.ItemsSource = _items;

                // Initialize the visual state
                UpdateVisualState();
                UpdateFlightRing();

                BlockAutoReboot.IsOn = IsAutomaticRebootBlockOn();
            }
        }

        /// <summary>
        /// State change notification handler
        ///     sender and args will be both be null because 
        ///     SystemUpdateManager is a static class
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void SystemUpdateManager_StateChanged(object sender, object args)
        {
            var action = _dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                UpdateVisualState();
            });
        }

        /// <summary>
        /// Update the MainPage visual state
        /// </summary>
        private void UpdateVisualState()
        {
            try
            {
                // Update the state text
                UpdateStateTextBlock.Text = GetResourceString(SystemUpdateManager.State.ToString());

                // Update the last update check time
                LastChecked.Text = SystemUpdateManager.LastUpdateCheckTime.ToString("G");

                // Change the VisualStateManager state based on the current SystemUpdateManagerState
                var state = SystemUpdateManager.State;
                Debug.WriteLine($"State={state}");
                switch (state)
                {
                    case SystemUpdateManagerState.Idle:
                    case SystemUpdateManagerState.Detecting:
                    case SystemUpdateManagerState.Downloading:
                    case SystemUpdateManagerState.Installing:
                    case SystemUpdateManagerState.RebootRequired:
                        VisualStateManager.GoToState(this, SystemUpdateManager.State.ToString(), false);
                        break;

                    case SystemUpdateManagerState.AttentionRequired:
                        AttentionRequiredTextBlock.Text = GetResourceString(SystemUpdateManager.AttentionRequiredReason.ToString());
                        VisualStateManager.GoToState(this, "AttentionRequired", false);
                        break;

                    default:
                        VisualStateManager.GoToState(this, "UnknownState", false);
                        break;
                }

                // Update progress for states with progress
                switch (SystemUpdateManager.State)
                {
                    case SystemUpdateManagerState.Downloading:
                        Debug.WriteLine($"Downloading={SystemUpdateManager.DownloadProgress}");
                        SessionDownloadProgressBar.Value = SystemUpdateManager.DownloadProgress;
                        break;
                    case SystemUpdateManagerState.Installing:
                        Debug.WriteLine($"Installing={SystemUpdateManager.InstallProgress}");
                        SessionDownloadProgressBar.Value = SystemUpdateManager.DownloadProgress;
                        SessionInstallProgressBar.Value = SystemUpdateManager.InstallProgress;
                        break;
                }

                // Update progress items
                switch (SystemUpdateManager.State)
                {
                    case SystemUpdateManagerState.Downloading:
                    case SystemUpdateManagerState.Installing:
                        foreach (var updateItem in SystemUpdateManager.GetUpdateItems())
                        {
                            var viewModelItem = _items.Where(x => x.Id == updateItem.Id).FirstOrDefault();
                            if (viewModelItem != null)
                            {
                                viewModelItem.Update(updateItem);
                            }
                            else
                            {
                                _items.Add(new UpdateItemViewModel(updateItem));
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
        
        /// <summary>
        /// The "Check for updates" button was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (SystemUpdateManager.State == SystemUpdateManagerState.Idle)
                {
                    SystemUpdateManager.StartInstall(SystemUpdateStartInstallAction.UpToReboot);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// The "Reboot Now" button was clicked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RebootNow_Click(object sender, RoutedEventArgs e)
        {
            if (SystemUpdateManager.State == SystemUpdateManagerState.RebootRequired)
            {
                SystemUpdateManager.RebootToCompleteInstall();
            }
        }

        /// <summary>
        /// Display user active hours and allow user to change user active hours
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeActiveHours_Click(object sender, RoutedEventArgs e)
        {
            StartTime.Time = SystemUpdateManager.UserActiveHoursStart;
            EndTime.Time = SystemUpdateManager.UserActiveHoursEnd;
            ActiveHoursErrorText.Visibility = Visibility.Collapsed;
            ActiveHoursPopup.IsOpen = true;
        }

        /// <summary>
        /// When the user clicks "Save" try to save the user active hours 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveActiveHours_Click(object sender, RoutedEventArgs e)
        {
            bool succeeded = SystemUpdateManager.TrySetUserActiveHours(StartTime.Time, EndTime.Time);
            if (succeeded)
            {
                ActiveHoursPopup.IsOpen = false;
            }
            else
            {
                // Active hours not set display error message
                string format = GetResourceString("ActiveHoursErrorFormat");
                ActiveHoursErrorText.Text = String.Format(format, SystemUpdateManager.UserActiveHoursMax);
                ActiveHoursErrorText.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Cancel changing the user active hours
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelActiveHours_Click(object sender, RoutedEventArgs e)
        {
            ActiveHoursPopup.IsOpen = false;
        }

        /// <summary>
        /// Position the popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ActiveHoursBorder_Loaded(object sender, RoutedEventArgs e)
        {
            ActiveHoursPopup.VerticalOffset = (SettingsGrid.ActualWidth / 2) - (ActiveHoursBorder.ActualWidth / 2);
            ActiveHoursPopup.HorizontalOffset = (SettingsGrid.ActualHeight / 2) - (ActiveHoursBorder.ActualHeight / 2);
        }

        /// <summary>
        /// Update the flight ring UI to match the current state
        /// </summary>
        private void UpdateFlightRing()
        {
            var ring = Windows.System.Update.SystemUpdateManager.GetFlightRing();
            for (int i = 0; i < FlightRingCombo.Items.Count(); i++)
            {
                if (ring == FlightRingCombo.Items[i] as string)
                {
                    FlightRingCombo.SelectedIndex = i;
                    return;
                }
            }

            // if the current ring is non-empty and is not in the list save it to the list
            if (!String.IsNullOrEmpty(ring))
            {
                int index = FlightRingCombo.Items.Count;
                FlightRingCombo.Items.Insert(index, ring);
                FlightRingCombo.SelectedIndex = index;
                return;
            }

            FlightRingCombo.SelectedIndex = 0;
        }

        /// <summary>
        /// Handle flight ring selection change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FlightRingCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var oldRing = SystemUpdateManager.GetFlightRing();
            var newRing = e.AddedItems[0].ToString();
            Debug.WriteLine($"newRing={newRing} oldRing={oldRing}");

            if (oldRing != newRing)
            {
                if (newRing == "None")
                {
                    // only set if previous ring was not null or empty
                    if (!String.IsNullOrEmpty(oldRing))
                    {
                        Windows.System.Update.SystemUpdateManager.SetFlightRing(String.Empty);
                    }
                }
                else
                {
                    Windows.System.Update.SystemUpdateManager.SetFlightRing(newRing);
                }
            }
        }

        /// <summary>
        /// Show the last error popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastError_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var info = SystemUpdateManager.LastErrorInfo;
                if (SystemUpdateManager.LastErrorInfo.ExtendedError == null)
                {
                    NoErrorText.Visibility = Visibility.Visible;
                    LastErrorInfoPanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    NoErrorText.Visibility = Visibility.Collapsed;
                    LastErrorInfoPanel.Visibility = Visibility.Visible;
                    ErrorStateTextBlock.Text = GetResourceString(info.State.ToString());
                    HResultTextBlock.Text = (info.ExtendedError == null) ? "No Error Data" : info.ExtendedError.Message;
                    IsInteractiveTextBlock.Text = GetResourceString(info.IsInteractive ? "Yes" : "No");
                }
                LastErrorInfoPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        /// <summary>
        /// Position the last error popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LastErrorInfoBorder_Loaded(object sender, RoutedEventArgs e)
        {
            LastErrorInfoPopup.VerticalOffset = (SettingsGrid.ActualWidth / 2) - (LastErrorInfoBorder.ActualWidth / 2);
            LastErrorInfoPopup.HorizontalOffset = (SettingsGrid.ActualHeight / 2) - (LastErrorInfoBorder.ActualHeight / 2);
        }

        /// <summary>
        /// Close the last error popup
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CloseLastErrorInfoButton_Click(object sender, RoutedEventArgs e)
        {
            LastErrorInfoPopup.IsOpen = false;
        }

        /// <summary>
        /// Return true if automatic update reboot is blocked.  False otherwise.
        /// </summary>
        /// <returns></returns>
        bool IsAutomaticRebootBlockOn()
        {
            var ids = SystemUpdateManager.GetAutomaticRebootBlockIds();
            return (ids.Count > 0);
        }

        /// <summary>
        /// Block automatic reboots until unblocked or until 
        /// automatic reboot is forced by system update policies
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BlockAutoReboot_Toggled(object sender, RoutedEventArgs e)
        {
            if (BlockAutoReboot.IsOn)
            {
                await SystemUpdateManager.BlockAutomaticRebootAsync(Guid.NewGuid().ToString());
            }
            else
            {
                var ids = SystemUpdateManager.GetAutomaticRebootBlockIds();
                foreach(var id in ids)
                {
                    bool unblocked = await SystemUpdateManager.UnblockAutomaticRebootAsync(id);
                }
            }
        }

        /// <summary>
        /// Get a resource string for the given resource name
        /// </summary>
        /// <param name="resourceName"></param>
        /// <returns></returns>
        internal static string GetResourceString(string resourceName)
        {
            var resourceLoader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView();
            string theResourceString = resourceLoader.GetString(resourceName);
            if (String.IsNullOrEmpty(theResourceString))
            {
                theResourceString = "Unknown";
            }
            return theResourceString;
        }
    }
}
