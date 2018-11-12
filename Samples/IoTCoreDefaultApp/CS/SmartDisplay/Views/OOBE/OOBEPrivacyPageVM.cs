// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.ViewModels
{
    public class OOBEPrivacyPageVM : BaseViewModel
    {
        public ObservableCollection<TelemetryLevelDisplay> TelemetryLevelCollection { get; } = new ObservableCollection<TelemetryLevelDisplay>();

        public TelemetryLevelDisplay SelectedItem
        {
            get { return GetStoredProperty<TelemetryLevelDisplay>(); }
            set { SetStoredProperty(value); }
        }

        public string SetTelemetryLevelResult
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string NextButtonText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        private RelayCommand _nextButtonCommand;
        public ICommand NextButtonCommand
        {
            get
            {
                return _nextButtonCommand ??
                    (_nextButtonCommand = new RelayCommand(unused =>
                    {
                        NavigateNext();
                    }));
            }
        }

        private IOOBEWindowService _windowService;
        
        public OOBEPrivacyPageVM()
        {
            PopulateTelemetryLevels();
            NextButtonText = Common.GetLocalizedText("AcceptButton/Content");
        }

        public void SetUpVM(IOOBEWindowService windowService)
        {
            _windowService = windowService;
        }

        private void PopulateTelemetryLevels()
        {
            InvokeOnUIThread(() =>
            {
                TelemetryLevelCollection.Clear();

                // Full telemetry
                TelemetryLevelCollection.Add(new TelemetryLevelDisplay
                {
                    Level = DevicePortalUtil.FullTelemetryValue,
                    Icon = "\uF4DC",
                    Title = Common.GetLocalizedText("FullTelemetryTitle/Text"),
                    Description = Common.GetLocalizedText("FullTelemetryDescription/Text"),
                });

                // Basic telemetry
                TelemetryLevelCollection.Add(new TelemetryLevelDisplay
                {
                    Level = DevicePortalUtil.BasicTelemetryValue,
                    Icon = "\uE9D9",
                    Title = Common.GetLocalizedText("BasicTelemetryTitle/Text"),
                    Description = Common.GetLocalizedText("BasicTelemetryDescription/Text"),
                });
            });
        }

        public async void ListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is TelemetryLevelDisplay item)
            {
                switch (item.Level)
                {
                    case DevicePortalUtil.FullTelemetryValue:
                    case DevicePortalUtil.BasicTelemetryValue:
                        // Clear the current status
                        SetTelemetryLevelResult = string.Empty;

                        // Display error if not logged in or level change failed
                        if (!await LoginPopupControl.SignInAsync(Common.GetLocalizedText("PrivacySignInDescription")) ||
                            !await UpdatePrivacyLevelAsync(item.Level))
                        {
                            SetTelemetryLevelResult = Common.GetLocalizedText("PrivacyLevelChangeFailed");
                            RevertToPreviousLevel();
                        }
                        break;
                }
            }
        }

        private int _previousLevel = DevicePortalUtil.InvalidTelemetryValue;
        private async Task<bool> UpdatePrivacyLevelAsync(int level)
        {
            var cred = DevicePortalUtil.GetCredential();
            if (cred != null)
            {
                if (await DevicePortalUtil.SetTelemetryLevelAsync(cred.UserName, cred.Password, level))
                {
                    _previousLevel = level;
                    return true;
                }
            }

            return false;
        }

        private void RevertToPreviousLevel()
        {
            SelectedItem = TelemetryLevelCollection.FirstOrDefault(x => x.Level == _previousLevel);
        }

        /// <summary>
        /// Advances to the next OOBE page
        /// </summary>
        private void NavigateNext()
        {
            _windowService.Navigate(typeof(OOBEPermissionsPage));
        }
    }
}
