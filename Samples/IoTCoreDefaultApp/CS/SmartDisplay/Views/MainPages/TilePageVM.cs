// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Windows.System.Threading;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.ViewModels
{
    public class TilePageVM : BaseViewModel
    {
        public ObservableCollection<TilePageCategory> TilePageCollection { get; } = new ObservableCollection<TilePageCategory>();

        private SettingsProvider _settings;
        private ThreadPoolTimer _updateTimer;
        private readonly TimeSpan _updateDelay = TimeSpan.FromSeconds(1);

        public TilePageVM() : base()
        {
            _settings = App.Settings;
        }

        public bool SetUpVM()
        {
            PopulateCommandBar();

            _settings.SettingsUpdated += Settings_SettingsUpdated;

            // Populate the page if it hasn't been populated yet
            if (TilePageCollection.Count == 0)
            {
                UpdatePages();
            }

            return true;
        }

        public void TearDownVM()
        {
            _settings.SettingsUpdated -= Settings_SettingsUpdated;
            _updateTimer?.Cancel();
            _updateTimer = null;
        }

        public void UpdatePages(ThreadPoolTimer timer = null)
        {
            _updateTimer?.Cancel();
            _updateTimer = null;

            InvokeOnUIThread(async () =>
            {
                TilePageCollection.Clear();

                foreach (var category in PageUtil.GetPagesByCategory())
                {
                    // Display the category only if there are pages in it
                    if (category.Value.Length == 0)
                    {
                        continue;
                    }

                    // Only display WDP pages if WDP is enabled
                    if (category.Key == PageUtil.WdpCategory)
                    {
                        // Check if Device Portal is enabled. Call this asynchronously so we don't block the UI if we have to make a network call.
                        bool wdpEnabled = false;
                        await ThreadPool.RunAsync((s) => wdpEnabled = DevicePortalUtil.IsDevicePortalEnabled(), WorkItemPriority.Normal);

                        if (!wdpEnabled)
                        {
                            continue;
                        }
                    }

                    var gridItems = new List<TileGridItem>();
                    foreach (var descriptor in category.Value)
                    {
                        gridItems.Add(new TileGridItem(descriptor, useMDL2: _settings.UseMDL2Icons)
                        {
                            Width = _settings.AppTileWidth,
                            Height = _settings.AppTileHeight,
                            BackgroundColor = new SolidColorBrush(_settings.TileColor)
                        });
                    }

                    TilePageCollection.Add(new TilePageCategory
                    {
                        Category = category.Key,
                        Pages = gridItems,
                    });
                }
            });
        }

        private void PopulateCommandBar()
        {
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            PageService?.AddCommandBarButton(PageUtil.CreatePageSettingCommandBarButton(
                PageService,
                new TileSettingsControl
                {
                    Width = Constants.DefaultSidePaneContentWidth,
                    Background = new SolidColorBrush(Colors.Transparent),
                },
                Common.GetLocalizedText("TileSettingHeader/Text")));
        }

        private readonly List<string> _tilePageSettings = new List<string>
        {
            "AppTileWidth",
            "AppTileHeight",
            "TileColor",
            "UseMDL2Icons",
        };
        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (_tilePageSettings.Contains(args.Key))
            {
                // Delay the UI refresh in case a lot of updates arrive at the same time
                _updateTimer = ThreadPoolTimer.CreateTimer(UpdatePages, _updateDelay);
            }
        }
    }

    public class TilePageCategory
    {
        public string Category { get; set; }
        public IEnumerable<TileGridItem> Pages { get; set; }
    }
}
