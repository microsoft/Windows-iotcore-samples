// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Presenters;
using SmartDisplay.Utils;
using SmartDisplay.Views.DevicePortal;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SmartDisplay.ViewModels.DevicePortal
{
    public class WindowsUpdatePageVM : BaseViewModel
    {
        #region UI properties
        public ObservableCollection<InfoDisplayData> InfoCollection { get; } = new ObservableCollection<InfoDisplayData>();
        #endregion

        public WindowsUpdatePageVM() : base()
        {
        }

        public async void SignInCompleted(object sender, SignInCompletedArgs args)
        {
            if (args.IsSuccessful)
            {
                PageService?.ShowLoadingPanel();
                try
                {
                    await UpdateInfoAsync();
                }
                finally
                {
                    PageService?.HideLoadingPanel();
                }

                PageService?.AddDevicePortalButtons(typeof(WindowsUpdatePage));
            }

            if (args.IsUserInitiated)
            {
                PageService?.ShowNotification((args.IsSuccessful) ? Common.GetLocalizedText("DevicePortalSignInSuccess") : Common.GetLocalizedText("DevicePortalSignInError"));
            }
        }

        private async Task UpdateInfoAsync()
        {
            string nl = Environment.NewLine;
            var cred = DevicePortalUtil.GetCredential();
            if (cred != null)
            {
                InfoCollection.Clear();

                var status = await DevicePortalUtil.GetWindowsUpdateStatusAsync(cred.UserName, cred.Password);
                if (status != null)
                {
                    InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("LastCheckedText"), status.LastCheckTime));
                    InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("LastUpdatedText"), status.LastUpdateTime));
                    InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("UpdatedStateText"), DevicePortalUtil.GetUpdateState(status.UpdateState)));
                    InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("StatusMessageText"), status.UpdateStatusMessage)); // UpdateStatusMessage is not localized
                }
            }
            else
            {
                App.LogService.Write("Credential is null", Windows.Foundation.Diagnostics.LoggingLevel.Warning);
            }
        }
    }
}
