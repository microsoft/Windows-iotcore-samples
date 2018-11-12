// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;
using Windows.Management.Deployment;
using Windows.Storage.Streams;
using Windows.System.Threading;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.ViewModels
{
    public class AppLauncherPageVM : BaseViewModel
    {
        public List<TileGridItem> AppCollection
        {
            get { return GetStoredProperty<List<TileGridItem>>() ?? new List<TileGridItem>(); }
            set { SetStoredProperty(value); }
        }

        private SettingsProvider _settings;
        private ThreadPoolTimer _updateTimer;
        private readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(1);

        private static List<string> ExcludedApps = new List<string>
        {
            "Work or school account",
            "Email and accounts",
            "Microsoft account",
            "Sign In",
            "Purchase Dialog",
            "IoTOnboardingTask",
            "IoTUAPOOBE",
            "ZWaveHeadlessAdapterApp",
            "ZWave Adapter Headless Host",
            "IOTCoreDefaultApplication",
            "IoTCoreDefaultAppUnderTest",
            "NoUIEntryPoints-DesignMode", // Development Mode debugging packages
            Package.Current.DisplayName,
        };

        public AppLauncherPageVM() : base()
        {
            _settings = App.Settings;
        }

        public bool SetUpVM()
        {
            _settings.SettingsUpdated += Settings_SettingsUpdated;

            // Populate the page if it hasn't been populated yet
            if (AppCollection.Count == 0)
            {
                UpdateApps();
            }

            return true;
        }

        public void TearDownVM()
        {
            _settings.SettingsUpdated -= Settings_SettingsUpdated;
            _updateTimer?.Cancel();
            _updateTimer = null;
        }

        private void UpdateApps(ThreadPoolTimer timer = null)
        {
            _updateTimer?.Cancel();
            _updateTimer = null;

            InvokeOnUIThread(async () =>
            {
                try
                {
                    PageService.ShowLoadingPanel();

                    var tiles = new List<TileGridItem>();

                    // <rescap:Capability Name="packageQuery" />
                    var packageManager = new PackageManager();
                    var packages = packageManager.FindPackagesForUserWithPackageTypes("", PackageTypes.Main);
                    foreach (var package in packages)
                    {
                        IReadOnlyList<AppListEntry> appList = null;
                        try
                        {
                            appList = await package.GetAppListEntriesAsync();
                        }
                        catch
                        {
                            // Ignore bad packages
                            continue;
                        }

                        foreach (AppListEntry entry in appList)
                        {
                            try
                            {
                                var entryName = entry.DisplayInfo.DisplayName;
                                if (ExcludedApps.Contains(entryName))
                                {
                                    continue;
                                }

                                var tile = new TileGridItem
                                {
                                    Width = _settings.AppTileWidth,
                                    Height = _settings.AppTileHeight,
                                    Icon = PageUtil.DefaultPageIcon,
                                    Title = entryName,
                                    TitleTextWrapping = TextWrapping.Wrap,
                                    BackgroundColor = new SolidColorBrush(_settings.TileColor),
                                    Data = entry,
                                };

                                var logo = entry.DisplayInfo.GetLogo(new Windows.Foundation.Size(150.0, 150.0));
                                tile.Image = new BitmapImage();
                                using (IRandomAccessStreamWithContentType logoStream = await logo.OpenReadAsync())
                                {
                                    await tile.Image.SetSourceAsync(logoStream);
                                }

                                tiles.Add(tile);
                            }
                            catch (Exception ex)
                            {
                                // Ignore bad apps in this package
                                System.Diagnostics.Debug.WriteLine(ex.ToString());
                            }
                        }
                    }

                    AppCollection = tiles;
                }
                catch (Exception ex)
                {
                    AppService.LogService.WriteException(ex);
                }
                finally
                {
                    PageService.HideLoadingPanel();
                }
            });
        }

        private readonly List<string> _tilePageSettings = new List<string>
        {
            "AppTileWidth",
            "AppTileHeight",
            "TileColor",
        };
        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (_tilePageSettings.Contains(args.Key))
            {
                // Delay the UI refresh in case a lot of updates arrive at the same time
                _updateTimer = ThreadPoolTimer.CreateTimer(UpdateApps, _updateDelay);
            }
        }
    }

    public class AppLauncherItem
    {
        public string Category { get; set; }
        public IEnumerable<TileGridItem> Pages { get; set; }
    }
}
