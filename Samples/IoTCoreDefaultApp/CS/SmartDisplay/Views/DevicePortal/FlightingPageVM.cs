// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Presenters;
using SmartDisplay.Utils;
using SmartDisplay.Views.DevicePortal;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SmartDisplay.ViewModels.DevicePortal
{
    public class FlightingPageVM : BaseViewModel
    {
        #region UI properties
        public List<string> FlightRings
        {
            get { return GetStoredProperty<List<string>>(); }
            set { SetStoredProperty(value); }
        }

        public string CurrentFlightRing
        {
            get { return GetStoredProperty<string>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    UpdateFlightRing();
                }
            }
        }
        public ObservableCollection<InfoDisplayData> InfoCollection { get; } = new ObservableCollection<InfoDisplayData>();
        #endregion

        #region Localized UI strings

        public string CurrentRingText { get; } = Common.GetLocalizedText("CurrentRingText") + ": ";

        #endregion
        private SemaphoreSlim _updateSemaphore { get; } = new SemaphoreSlim(1);

        public FlightingPageVM() : base()
        {
            FlightRings = DevicePortalUtil.GetRings().ToList();
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

                PageService?.AddDevicePortalButtons(typeof(FlightingPage));
            }

            if (args.IsUserInitiated)
            {
                PageService?.ShowNotification((args.IsSuccessful) ? Common.GetLocalizedText("DevicePortalSignInSuccess") : Common.GetLocalizedText("DevicePortalSignInError"));
            }
        }

        private async void UpdateFlightRing()
        {
            if (CurrentFlightRing is string ring)
            {
                var cred = DevicePortalUtil.GetCredential();
                if (cred == null)
                {
                    return;
                }

                var output = await DevicePortalUtil.SetFlightRingAsync(cred.UserName, cred.Password, ring);
                if (output?.ExitCode == 0)
                {
                    PageService.ShowNotification(string.Format(Common.GetLocalizedText("FlightingRingChange"), ring));
                    await UpdateInfoAsync();
                }
                else
                {
                    PageService.ShowNotification(Common.GetLocalizedText("FlightingRingError"));
                }
            }
        }

        private async Task UpdateInfoAsync()
        {
            try
            {
                await _updateSemaphore.WaitAsync();

                InfoCollection.Clear();
                string nl = Environment.NewLine;
                var cred = DevicePortalUtil.GetCredential();
                if (cred != null)
                {
                    var ring = await DevicePortalUtil.GetFlightRingAsync(cred.UserName, cred.Password);
                    if (ring != null)
                    {
                        CurrentFlightRing = ring;
                    }
                    var telemetryLevelOutput = await DevicePortalUtil.GetTelemetryLevelAsync(cred.UserName, cred.Password);
                    if (telemetryLevelOutput != DevicePortalUtil.InvalidTelemetryValue)
                    {
                        InfoCollection.Add(new InfoDisplayData(Common.GetLocalizedText("TelemetryLevelText") + ": ",
                            DevicePortalUtil.TelemetryLevelToFriendlyName(telemetryLevelOutput)));
                    }
                }
                else
                {
                    App.LogService.Write("Credential is null", Windows.Foundation.Diagnostics.LoggingLevel.Warning);
                }
            }
            finally
            {
                _updateSemaphore.Release();
            }
        }
    }
}
