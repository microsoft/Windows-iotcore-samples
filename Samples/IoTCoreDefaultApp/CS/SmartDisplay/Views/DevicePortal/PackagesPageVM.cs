// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views.DevicePortal;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SmartDisplay.ViewModels.DevicePortal
{
    public class PackagesPageVM : BaseViewModel
    {
        #region UI properties
        public List<InstalledPackage> Items
        {
            get { return GetStoredProperty<List<InstalledPackage>>(); }
            set { SetStoredProperty(value); }
        }
        #endregion

        public PackagesPageVM() : base()
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

                PageService?.AddDevicePortalButtons(typeof(PackagesPage));
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

            var packages = await DevicePortalUtil.GetInstalledPackagesAsync(cred.UserName, cred.Password);
            if (packages != null)
            {
                Items = packages.InstalledPackages.Where(x => x.AppListEntry == 0).ToList();
            }
        }
    }
}
