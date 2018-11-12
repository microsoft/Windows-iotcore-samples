// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Services.Store;
using Windows.System.Threading;

namespace SmartDisplay.Updates
{
    #region UpdateEventArgs
    public class StorePackageUpdateStatusEventArgs : EventArgs
    {
        public StorePackageUpdateStatus Status;
    }

    public class StorePackageUpdateStateEventArgs : EventArgs
    {
        public StorePackageUpdateState State;
    }

    public class UpdateRefreshEventArgs : EventArgs
    {
        public UpdateRefreshEventArgs(UpdateStage stage, bool result, IEnumerable<StorePackageUpdate> updates = null)
        {
            UpdateStage = stage;
            Success = result;
            Updates = updates;
            UtcTime = DateTime.UtcNow;
        }

        public UpdateStage UpdateStage { get; set; }
        public bool Success { get; set; }
        public DateTime UtcTime { get; set; }
        public IEnumerable<StorePackageUpdate> Updates { get; set; }
    }
    #endregion

    #region UpdateHandlers
    public delegate void StorePackageUpdateStatusHandler(object sender, StorePackageUpdateStatusEventArgs e);
    public delegate void StorePackageUpdateStateHandler(object sender, StorePackageUpdateStateEventArgs e);
    public delegate void UpdatesRefreshHandler(object sender, UpdateRefreshEventArgs e);
    #endregion 

    public enum UpdateStage
    {
        Fetch, Download, Install
    }

    public static class UpdateManager
    {
        public static int PendingUpdatesCount { private set; get; }
        public static event StorePackageUpdateStatusHandler OnUpdateDownloadProgress;
        public static event StorePackageUpdateStatusHandler OnUpdateInstallProgress;
        public static event StorePackageUpdateStateHandler OnUpdateOperationError;

        public static event UpdatesRefreshHandler OnUpdatesRefresh;

        private static ThreadPoolTimer _appUpdateCheckTimer;
        private static StoreContext _storeContext;

        public static void StartAppUpdateChecker(TimeSpan timeSpan)
        {
            if (_appUpdateCheckTimer == null)
            {
                _appUpdateCheckTimer = ThreadPoolTimer.CreateTimer(AppUpdateCheck, timeSpan);
            }
        }

        public static void StopAppUpdateChecker()
        {
            _appUpdateCheckTimer?.Cancel();
            _appUpdateCheckTimer = null;
        }

        private static StoreContext GetStoreContext()
        {
            if (_storeContext == null)
            {
                _storeContext = StoreContext.GetDefault();
            }

            return _storeContext;
        }

        public static async void AppUpdateCheck(ThreadPoolTimer timer)
        {
            var updates = await FetchUpdatesAsync();
            if (updates != null && updates.Count() > 0)
            {
                var result = TryAutoUpdateInstall(updates);
            }
            
            _appUpdateCheckTimer = ThreadPoolTimer.CreateTimer(AppUpdateCheck, TimeSpan.FromHours(1));
        }

        public static async Task TryAutoUpdateInstall(IEnumerable<StorePackageUpdate> updates)
        {
            if (App.Settings.AutoUpdateInstallEnabled)
            {
                TimeSpan now = DateTime.Now.TimeOfDay;
                TimeSpan start = App.Settings.ActiveTimeStart;
                TimeSpan end = App.Settings.ActiveTimeEnd;

                int diff = start.CompareTo(end);
                if (diff < 0)
                {
                    // Start < End, check if current time outside
                    if (now.CompareTo(start) < 0 ^ now.CompareTo(end) > 0)
                    {
                        await TrySilentDownloadAndInstallAync(updates);
                    }
                }

                if (diff > 0)
                {
                    // End < Start, check if current time inside
                    if (now.CompareTo(start) < 0 && now.CompareTo(end) > 0)
                    {
                        await TrySilentDownloadAndInstallAync(updates);
                    }
                }
            }
        }

        public static async Task<IEnumerable<StorePackageUpdate>> FetchUpdatesAsync()
        {
            App.LogService.Write("Fetching app updates...");

            var context = GetStoreContext();
            IEnumerable<StorePackageUpdate> updates;

            try
            {
                updates = await context.GetAppAndOptionalStorePackageUpdatesAsync();
                PendingUpdatesCount = updates.Count();
                App.LogService.Write("Pending app updates: " + PendingUpdatesCount);
            }
            catch (Exception ex)
            {
                App.LogService.WriteException(ex);
                return null;
            }

            OnUpdatesRefresh?.Invoke(null, new UpdateRefreshEventArgs(UpdateStage.Fetch, true, updates));

            return updates;
        }

        public static async Task<bool> DownloadUpdatesAsync(IEnumerable<StorePackageUpdate> updates)
        {
            App.LogService.Write("Downloading app updates...");

            bool success = false;
            StoreContext context = GetStoreContext();

            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> downloadOperation = context.RequestDownloadStorePackageUpdatesAsync(updates);

            downloadOperation.Progress = (asyncInfo, progress) =>
            {
                LogProgress(progress);
                OnUpdateDownloadProgress?.Invoke(null, new StorePackageUpdateStatusEventArgs()
                {
                    Status = progress
                });
            };

            StorePackageUpdateResult result = await downloadOperation.AsTask();

            switch (result.OverallState)
            {
                case StorePackageUpdateState.Completed:
                    success = true;
                    break;
                case StorePackageUpdateState.Canceled:
                case StorePackageUpdateState.Deploying:
                case StorePackageUpdateState.Downloading:
                case StorePackageUpdateState.Pending:
                    break;
                default:
                    OnUpdateOperationError?.Invoke(null, new StorePackageUpdateStateEventArgs()
                    {
                        State = result.OverallState
                    });
                    break;
            }

            OnUpdatesRefresh?.Invoke(null, new UpdateRefreshEventArgs(UpdateStage.Download, success));
            return success;
        }

        public static async Task InstallUpdatesAsync(IEnumerable<StorePackageUpdate> updates)
        {
            App.LogService.Write("Installing updates...");

            IAsyncOperationWithProgress<StorePackageUpdateResult, StorePackageUpdateStatus> installOperation = null;
            StoreContext context = GetStoreContext();

            installOperation = context.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);

            installOperation.Progress = (asyncInfo, progress) =>
            {
                LogProgress(progress);
                OnUpdateInstallProgress?.Invoke(null, new StorePackageUpdateStatusEventArgs()
                {
                    Status = progress
                });
            };

            StorePackageUpdateResult result = await installOperation.AsTask();
            switch (result.OverallState)
            {
                case StorePackageUpdateState.Completed:
                    // Should never hit this state as the install process will terminate the app
                    App.LogService.Write("App should have terminated in order to begin app install...");
                    break;
                case StorePackageUpdateState.Canceled:
                case StorePackageUpdateState.Deploying:
                case StorePackageUpdateState.Downloading:
                case StorePackageUpdateState.Pending:
                    break;
                default:
                    OnUpdateOperationError?.Invoke(null, new StorePackageUpdateStateEventArgs()
                    {
                        State = result.OverallState
                    });
                    break;
            }

            OnUpdatesRefresh?.Invoke(null, new UpdateRefreshEventArgs(UpdateStage.Install, false));
        }

        public static async Task TrySilentDownloadAndInstallAync(IEnumerable<StorePackageUpdate> updates)
        {
            App.LogService.Write("Trying to silently download and install updates...");

            // Check if silent API supported (RS4+)
            if (Windows.Foundation.Metadata.ApiInformation.IsMethodPresent("Windows.Services.Store.StoreContext", "TrySilentDownloadAndInstallStorePackageUpdatesAsync"))
            {
                StoreContext context = GetStoreContext();
                StorePackageUpdateResult result = await context.TrySilentDownloadAndInstallStorePackageUpdatesAsync(updates).AsTask();

                switch (result.OverallState)
                {
                    case StorePackageUpdateState.Completed:
                        // Should never hit this state as the install process will terminate the app
                        App.LogService.Write("App should have terminated in order to begin app install...");
                        break;
                    case StorePackageUpdateState.Canceled:
                    case StorePackageUpdateState.Deploying:
                    case StorePackageUpdateState.Downloading:
                    case StorePackageUpdateState.Pending:
                        break;
                    default:
                        App.TelemetryService.WriteEvent("AppUpdateError", new
                        {
                            StorePackageUpdateState = result.OverallState
                        });
                        break;
                }
            }
        }

        private static void LogProgress(StorePackageUpdateStatus progress)
        {
            string packageName = progress.PackageFamilyName;

            switch (progress.PackageUpdateState)
            {
                case StorePackageUpdateState.Pending:
                    App.LogService.Write("Package download for " + packageName + " is pending");
                    break;
                case StorePackageUpdateState.Downloading:
                    App.LogService.Write("Package download for " + packageName + " is downloading");
                    break;
                case StorePackageUpdateState.Completed:
                    App.LogService.Write("Package download for " + packageName + " is completed");
                    break;
                case StorePackageUpdateState.Canceled:
                    App.LogService.Write("Package download for " + packageName + " is cancelled");
                    break;
                case StorePackageUpdateState.ErrorLowBattery:
                    App.LogService.Write("Package download for " + packageName + " has stopped due to low battery");
                    break;
                case StorePackageUpdateState.ErrorWiFiRecommended:
                    App.LogService.Write("Package download for " + packageName + " has stopped due to no Wi-Fi recommendation");
                    break;
                case StorePackageUpdateState.ErrorWiFiRequired:
                    App.LogService.Write("Package download for " + packageName + " has stopped due to Wi-Fi requirement");
                    break;
                case StorePackageUpdateState.OtherError:
                    App.LogService.Write("Package download for " + packageName + " has encountered an error");
                    break;
            }
        }
    }
}