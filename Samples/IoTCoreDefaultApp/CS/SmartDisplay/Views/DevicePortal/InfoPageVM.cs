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
    public class InfoPageVM : BaseViewModel
    {
        #region UI properties

        public ObservableCollection<InfoDisplayData> InfoCollection { get; } = new ObservableCollection<InfoDisplayData>();

        #endregion

        public InfoPageVM() : base()
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

                PageService?.AddDevicePortalButtons(typeof(InfoPage));
            }

            if (args.IsUserInitiated)
            {
                PageService?.ShowNotification((args.IsSuccessful) ? Common.GetLocalizedText("DevicePortalSignInSuccess") : Common.GetLocalizedText("DevicePortalSignInError"));
            }
        }

        private async Task UpdateInfoAsync()
        {
            var cred = DevicePortalUtil.GetCredential();
            if (cred == null)
            {
                return;
            }

            var osInfo = await DevicePortalUtil.GetOsInfoAsync(cred.UserName, cred.Password);
            if (osInfo != null)
            {
                string nl = Environment.NewLine;

                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("ComputerNameText") + ": ", osInfo.ComputerName));
                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("LanguageText") + ": ", osInfo.Language));
                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("OSEditionText") + ": ", osInfo.OsEdition));
                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("OSEditionIDText") + ": ", osInfo.OsEditionId));
                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("OSVersionText") + ": ", osInfo.OsVersion));
                InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("PlatformText") + ": ", osInfo.Platform));
            }
        }
    }
}
