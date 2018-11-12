// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SmartDisplay.ViewModels.Settings
{
    public class DiagnosticSettingsVM : BaseViewModel
    {
        #region UI properties

        public bool IsBasicLevelSelected
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value) && value)
                {
                    var unused = TryUpdateAsync(DevicePortalUtil.BasicTelemetryValue);
                }
            }
        }

        public bool IsFullLevelSelected
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value) && value)
                {
                    var unused = TryUpdateAsync(DevicePortalUtil.FullTelemetryValue);
                }
            }
        }

        public string SetTelemetryLevelResult
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Commands
        
        private RelayCommand _hyperlinkCommand;
        public ICommand HyperlinkCommand
        {
            get
            {
                return _hyperlinkCommand ??
                    (_hyperlinkCommand = new RelayCommand((object parameter) =>
                    {
                        if (parameter is string hyperlinkType)
                        {
                            switch (hyperlinkType)
                            {
                                case "PrivacyStatement":
                                    PageService?.NavigateTo(typeof(WebBrowserPage), Constants.PrivacyStatementUrl);
                                    break;
                                case "LearnMore":
                                    PageService?.NavigateTo(typeof(WebBrowserPage), Constants.PrivacyLearnMoreUrl);
                                    break;
                            }
                        }
                    }));
            }
        }

        #endregion

        private int _previousLevel;

        public DiagnosticSettingsVM() : base()
        {
        }

        public async Task SetUpVM()
        {
            IsBasicLevelSelected = false;
            IsFullLevelSelected = false;
            SetTelemetryLevelResult = string.Empty;

            var cred = DevicePortalUtil.GetCredential();
            if (cred != null)
            {
                _previousLevel = await DevicePortalUtil.GetTelemetryLevelAsync(cred.UserName, cred.Password);
                if (_previousLevel == DevicePortalUtil.BasicTelemetryValue)
                {
                    IsBasicLevelSelected = true;
                }
                else if (_previousLevel == DevicePortalUtil.FullTelemetryValue)
                {
                    IsFullLevelSelected = true;
                }
            }
        }

        /// <summary>
        /// Tries to update the telemetry level and reverts to previous level if it fails
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        private async Task<bool> TryUpdateAsync(int level)
        {
            // Clear the current status
            SetTelemetryLevelResult = string.Empty;

            if (!await UpdatePrivacyLevelAsync(level))
            {
                SetTelemetryLevelResult = Common.GetLocalizedText("PrivacyLevelChangeFailed");
                await RevertToPreviousAsync();

                return false;
            }

            return true;
        }

        private async Task<bool> UpdatePrivacyLevelAsync(int level, bool promptUser = true)
        {
            // Return false if we're not signed in and we're not allowed to prompt the user
            if (!promptUser && !await DevicePortalUtil.IsSignedInAsync())
            {
                return false;
            }

            // Prompt the user for credentials if not logged in
            if (await LoginPopupControl.SignInAsync(Common.GetLocalizedText("PrivacySignInDescription")))
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
            }

            return false;
        }

        /// <summary>
        /// Reverts to the previous level, but if it fails for some reason (e.g. password
        /// was changed), it clears both radio buttons
        /// </summary>
        /// <returns></returns>
        private async Task<bool> RevertToPreviousAsync()
        {
            if (await UpdatePrivacyLevelAsync(_previousLevel, false))
            {
                IsBasicLevelSelected = _previousLevel == DevicePortalUtil.BasicTelemetryValue;
                IsFullLevelSelected = _previousLevel == DevicePortalUtil.FullTelemetryValue;
                return true;
            }
            else
            {
                IsBasicLevelSelected = IsFullLevelSelected = false;
                return false;
            }
        }
    }
}
