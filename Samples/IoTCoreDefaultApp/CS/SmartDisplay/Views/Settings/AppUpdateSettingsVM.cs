// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Updates;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Windows.Services.Store;

namespace SmartDisplay.ViewModels.Settings
{
    public class AppUpdateSettingsVM : BaseViewModel
    {
        #region UI properties and commands

        public string AppVersionInfoText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string UpdateInfoText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string LastUpdateCheckCountText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string LastUpdateCheckTimeText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string UpdateButtonText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public bool UpdateButtonsEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool AdvanceButtonsEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool AdvancedOptionsEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                UpdateButtonsEnabled = !value;
                AdvanceButtonsEnabled = value;
                SetStoredProperty(value);
            }
        }

        public bool CheckUpdateButtonsEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool DownloadButtonEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool InstallButtonEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public double UpdateProgressBarValue
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public bool AutoUpdateInstallEnabled
        {
            get { return App.Settings.AutoUpdateInstallEnabled; }
            set
            {
                if (App.Settings.AutoUpdateInstallEnabled != value)
                {
                    App.Settings.AutoUpdateInstallEnabled = value;
                    SetStoredProperty(value);
                }
            }
        }

        public TimeSpan ActiveTimeStart
        {
            get { return App.Settings.ActiveTimeStart; }
            set
            {
                IsActiveTimeInvalidTextVisible = value == App.Settings.ActiveTimeEnd;
                if (!IsActiveTimeInvalidTextVisible)
                {
                    App.Settings.ActiveTimeStart = value;
                }
            }
        }

        public TimeSpan ActiveTimeEnd
        {
            get { return App.Settings.ActiveTimeEnd; }
            set
            {
                IsActiveTimeInvalidTextVisible = value == App.Settings.ActiveTimeStart;
                if (!IsActiveTimeInvalidTextVisible)
                {
                    App.Settings.ActiveTimeEnd = value;
                }
            }
        }

        public bool IsActiveTimeInvalidTextVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }


        private RelayCommand _updateCommand;
        public ICommand UpdateCommand
        {
            get
            {
                return _updateCommand ??
                    (_updateCommand = new RelayCommand(async unused =>
                    {
                        // Disable all buttons while command executes
                        ManualUpdateButtonsEnabled = false;

                        UpdateInfoText = CheckForUpdatesText;
                        PendingUpdates = await UpdateManager.FetchUpdatesAsync();

                        if (PendingUpdates != null && PendingUpdates.Count() > 0)
                        {
                            if (await UpdateManager.DownloadUpdatesAsync(PendingUpdates))
                            {
                                await UpdateManager.InstallUpdatesAsync(PendingUpdates);
                            }
                        }

                        ManualUpdateButtonsEnabled = true;
                    }));
            }
        }

        private RelayCommand _checkForUpdatesCommand;
        public ICommand CheckForUpdatesCommand
        {
            get
            {
                return _checkForUpdatesCommand ??
                    (_checkForUpdatesCommand = new RelayCommand(async unused =>
                    {
                        // Disable all buttons while command executes
                        ManualUpdateButtonsEnabled = false;

                        UpdateInfoText = CheckForUpdatesText;
                        PendingUpdates = await UpdateManager.FetchUpdatesAsync();

                        ManualUpdateButtonsEnabled = true;
                    }));
            }
        }

        private RelayCommand<bool> _downloadUpdatesCommand;
        public ICommand DownloadUpdatesCommand
        {
            get
            {
                return _downloadUpdatesCommand ??
                (_downloadUpdatesCommand = new RelayCommand<bool>(async (failSilent) =>
                {
                    // Disable all buttons while command executes
                    ManualUpdateButtonsEnabled = false;

                    if (PendingUpdates != null && PendingUpdates.Count() > 0)
                    {
                        await UpdateManager.DownloadUpdatesAsync(PendingUpdates);
                    }
                    else if (!failSilent)
                    {
                        AppService.DisplayDialog(AppUpdateDialogHeaderText, NoPendingDownloadsText);
                    }

                    ManualUpdateButtonsEnabled = true;
                }));
            }
        }

        private RelayCommand _installUpdatesCommand;
        public ICommand InstallUpdatesCommand
        {
            get
            {
                return _installUpdatesCommand ??
                    (_installUpdatesCommand = new RelayCommand(async unused =>
                    {
                        // Disable all buttons while command executes
                        ManualUpdateButtonsEnabled = false;

                        if (PendingUpdates != null && PendingUpdates.Count() > 0)
                        {
                            await UpdateManager.InstallUpdatesAsync(PendingUpdates);
                        }
                        else
                        {
                            AppService.DisplayDialog(AppUpdateDialogHeaderText, NoPendingInstallsText);
                        }

                        ManualUpdateButtonsEnabled = true;
                    }));
            }
        }

        #endregion

        #region Page properties and services 

        private IEnumerable<StorePackageUpdate> _pendingUpdates;
        private IEnumerable<StorePackageUpdate> PendingUpdates
        {
            get { return _pendingUpdates; }
            set
            {
                _pendingUpdates = value;
                OnPendingUpdatesChange();
            }
        }

        private bool _checkUpdateButtonsEnabled;
        private bool _downloadButtonEnabled;
        private bool _installButtonEnabled;
        private bool _manualUpdateButtonsEnabled;
        private bool ManualUpdateButtonsEnabled
        {
            get { return _manualUpdateButtonsEnabled; }
            set
            {
                if (value)
                {
                    CheckUpdateButtonsEnabled = _checkUpdateButtonsEnabled;
                    DownloadButtonEnabled = _downloadButtonEnabled;
                    InstallButtonEnabled = _installButtonEnabled;
                }
                else
                {
                    CheckUpdateButtonsEnabled = false;
                    DownloadButtonEnabled = false;
                    InstallButtonEnabled = false;
                }
                _manualUpdateButtonsEnabled = value;
            }
        }

        private void SaveUpdateCheckStatus(int updateCount, DateTime time)
        {
            if (updateCount >= 0)
            {
                string formatText = (updateCount == 1) ? UpdatePendingCountText : UpdatesPendingCountText;
                LastUpdateCheckCountText = string.Format(formatText, updateCount);
                UpdateButtonText = (updateCount == 0) ? UpdateButtonCheckText : UpdateButtonInstallText;
            }
            App.Settings.LastUpdateCheckCount = updateCount;

            if (time == DateTime.MinValue)
            {  
                LastUpdateCheckTimeText = AppUpdateNoLastUpdateTimeText;
            }
            else
            {
                App.Settings.LastUpdateCheckUtc = time;
                LastUpdateCheckTimeText = string.Format(AppUpdateLastUpdateCheckText, time.ToLocalTime().ToShortDateString(), time.ToLocalTime().ToShortTimeString());
            }
        }

        private ITelemetryService TelemetryService => AppService?.TelemetryService;

        #endregion

        #region Localized UI strings

        private string AppUpdateDialogHeaderText { get; } = Common.GetLocalizedText("AppUpdateDialogHeaderText");
        private string AppUpdateLastUpdateCheckText { get; } = Common.GetLocalizedText("AppUpdateLastUpdateCheckText");
        private string AppUpdateNoLastUpdateTimeText { get; } = Common.GetLocalizedText("AppUpdateNoLastUpdateTimeText");
        private string AppUpdateInfoFormat { get; } = Common.GetLocalizedText("AppUpdateInfoFormat");
        private string AppBuildText { get; } = Common.GetLocalizedText("AppBuildText");
        private string BatteryLowErrorText { get; } = Common.GetLocalizedText("BatteryLowErrorText");
        private string CheckForUpdatesText { get; } = Common.GetLocalizedText("CheckForUpdatesText");
        private string ConnectionErrorText { get; } = Common.GetLocalizedText("ConnectionErrorText");
        private string DownloadCompleteText { get; } = Common.GetLocalizedText("DownloadCompleteText");
        private string DownloadErrorText { get; } = Common.GetLocalizedText("DownloadErrorText");
        private string DownloadStartedText { get; } = Common.GetLocalizedText("DownloadText");
        private string InstallCompleteText { get; } = Common.GetLocalizedText("InstallCompleteText");
        private string InstallErrorText { get; } = Common.GetLocalizedText("InstallErrorText");
        private string InstallStartedText { get; } = Common.GetLocalizedText("InstallStartedText");
        private string NoPendingDownloadsText { get; } = Common.GetLocalizedText("NoPendingDownloadsText");
        private string NoPendingInstallsText { get; } = Common.GetLocalizedText("NoPendingInstallsText");
        private string NoUpdatesFoundText { get; } = Common.GetLocalizedText("NoUpdatesFoundText");
        private string UpdateButtonCheckText { get; } = Common.GetLocalizedText("UpdateButtonCheckText");
        private string UpdateButtonInstallText { get; } = Common.GetLocalizedText("UpdateButtonInstallText");
        private string UnknownErrorText { get; } = Common.GetLocalizedText("UnknownErrorText");
        private string UpdatesFoundText { get; } = Common.GetLocalizedText("UpdatesFoundText");
        private string UpdatePendingCountText { get; } = Common.GetLocalizedText("UpdatePendingCountText");
        private string UpdatesPendingCountText { get; } = Common.GetLocalizedText("UpdatesPendingCountText");

        #endregion

        public AppUpdateSettingsVM() : base()
        {
            // Set default width for settings panel
            Width = Constants.SettingsWidth;

            AppVersionInfoText = string.Format(AppUpdateInfoFormat, AppBuildText, Common.GetAppVersion());
            SaveUpdateCheckStatus(App.Settings.LastUpdateCheckCount, App.Settings.LastUpdateCheckUtc);
            AdvancedOptionsEnabled = false;

            ManualUpdateButtonsEnabled = true;
            CheckUpdateButtonsEnabled = true;
            DownloadButtonEnabled = false;
            InstallButtonEnabled = false;

            AutoUpdateInstallEnabled = App.Settings.AutoUpdateInstallEnabled;
            ActiveTimeStart = App.Settings.ActiveTimeStart;
            ActiveTimeEnd = App.Settings.ActiveTimeEnd;
        }

        public void SetUpVM()
        {
            UpdateManager.OnUpdateDownloadProgress += UpdateManager_OnUpdateDownloadProgress;
            UpdateManager.OnUpdateInstallProgress += UpdateManager_OnUpdateInstallProgress;
            UpdateManager.OnUpdateOperationError += UpdateManager_OnUpdateOperationError;
            UpdateManager.OnUpdatesRefresh += UpdateManager_OnUpdatesRefresh;
        }

        public void TearDownVM()
        {
            UpdateManager.OnUpdateDownloadProgress -= UpdateManager_OnUpdateDownloadProgress;
            UpdateManager.OnUpdateInstallProgress -= UpdateManager_OnUpdateInstallProgress;
            UpdateManager.OnUpdateOperationError -= UpdateManager_OnUpdateOperationError;
        }

        private void UpdateManager_OnUpdateDownloadProgress(object sender, StorePackageUpdateStatusEventArgs e)
        {
            switch (e.Status.PackageUpdateState)
            {
                case StorePackageUpdateState.Downloading:
                    UpdateProgress(e.Status.PackageDownloadProgress * 100, DownloadStartedText);
                    break;
                case StorePackageUpdateState.Completed:
                    UpdateProgress(100, DownloadCompleteText);
                    break;
                default:
                    break;
            }
        }

        private void UpdateManager_OnUpdateInstallProgress(object sender, StorePackageUpdateStatusEventArgs e)
        {
            switch (e.Status.PackageUpdateState)
            {
                case StorePackageUpdateState.Downloading:
                    UpdateProgress(e.Status.PackageDownloadProgress * 100, InstallStartedText);
                    break;
                case StorePackageUpdateState.Completed:
                    UpdateProgress(100, InstallCompleteText);
                    break;
                default:
                    break;
            }
        }

        private void UpdateManager_OnUpdateOperationError(object sender, StorePackageUpdateStateEventArgs e)
        {
            switch (e.State)
            {
                case StorePackageUpdateState.ErrorLowBattery:
                    AppService.DisplayDialog(AppUpdateDialogHeaderText, BatteryLowErrorText);
                    TelemetryService.WriteEvent("AppUpdateError", new
                    {
                        StorePackageUpdateState = StorePackageUpdateState.ErrorLowBattery
                    });
                    break;
                case StorePackageUpdateState.ErrorWiFiRecommended:
                    AppService.DisplayDialog(AppUpdateDialogHeaderText, ConnectionErrorText);
                    TelemetryService.WriteEvent("AppUpdateError", new
                    {
                        StorePackageUpdateState = StorePackageUpdateState.ErrorWiFiRecommended
                    });
                    break;
                case StorePackageUpdateState.ErrorWiFiRequired:
                    AppService.DisplayDialog(AppUpdateDialogHeaderText, ConnectionErrorText);
                    TelemetryService.WriteEvent("AppUpdateError", new
                    {
                        StorePackageUpdateState = StorePackageUpdateState.ErrorWiFiRequired
                    });
                    break;
                case StorePackageUpdateState.OtherError:
                    AppService.DisplayDialog(AppUpdateDialogHeaderText, UnknownErrorText);
                    TelemetryService.WriteEvent("AppUpdateError", new
                    {
                        StorePackageUpdateState = StorePackageUpdateState.OtherError
                    });
                    break;
            }
        }

        private void OnPendingUpdatesChange()
        {
            if (PendingUpdates == null)
            {
                UpdateInfoText = NoUpdatesFoundText;
            }
            else
            {
                UpdateInfoText = (PendingUpdates.Count() == 0) ? NoUpdatesFoundText : string.Format(UpdatesFoundText, PendingUpdates.Count());
            }
        }

        private void UpdateProgress(double value, string message)
        {
            UpdateProgressBarValue = value;
            UpdateInfoText = message;
        }

        private void UpdateManager_OnUpdatesRefresh(object sender, UpdateRefreshEventArgs e)
        {
            _checkUpdateButtonsEnabled = true;
            _downloadButtonEnabled = false;
            _installButtonEnabled = false;

            switch (e.UpdateStage)
            {
                case UpdateStage.Fetch:
                    if (e.Updates?.Count() > 0)
                    {
                        SaveUpdateCheckStatus(e.Updates.Count(), e.UtcTime);
                        _downloadButtonEnabled = true;
                        PendingUpdates = e.Updates;
                    }
                    else
                    {
                        SaveUpdateCheckStatus(0, e.UtcTime);
                        PendingUpdates = null;
                    }
                    break;
                case UpdateStage.Download:
                    _downloadButtonEnabled = true;
                    if (e.Success)
                    {
                        _installButtonEnabled = true;
                        UpdateInfoText = DownloadCompleteText;
                    }
                    else
                    {
                        UpdateInfoText = DownloadErrorText;
                    }
                    break;
                case UpdateStage.Install:
                    _downloadButtonEnabled = true;
                    _installButtonEnabled = true;
                    if (!e.Success)
                    {
                        UpdateInfoText = InstallErrorText;
                    }
                    break;
            }

            ManualUpdateButtonsEnabled = true;
        }
    }
}
