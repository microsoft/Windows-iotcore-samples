// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.ViewModels
{
    public class MainPageVM : BaseViewModel
    {
        public const double DefaultWidth = 800;
        public const double DefaultHeight = 480;
        public const double DefaultCompactPaneLength = 50;
        public const double FullscreenCompactPaneLength = 0;
        public const string DefaultCommandBarClosedDisplayMode = "Compact";
        public const string FullscreenCommandBarClosedDisplayMode = "Minimal";

        private bool _isFullscreen;

        private SettingsProvider Settings => SmartDisplay.AppService.GetForCurrentContext().Settings as SettingsProvider;
        private ILogService LogService => AppService.LogService;

        private SolidColorBrush OpenPaneColor { get; } = Application.Current.Resources["SystemControlBackgroundChromeMediumLowBrush"] as SolidColorBrush;
        private SolidColorBrush ClosedPaneColor { get; } = new SolidColorBrush(Colors.Black);

        public MainPageVM()
        {
            PageWidth = DefaultWidth;
            PageHeight = DefaultHeight;
            CompactPaneLength = DefaultCompactPaneLength;
            CommandBarClosedDisplayMode = DefaultCommandBarClosedDisplayMode;
            IsCommandBarVisible = true;
            IsNavBarVisible = true;

            _isFullscreen = false;
        }

        #region Navigation Sidebar
        
        public object TopSelectedNavBarItem
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }

        public object BottomSelectedNavBarItem
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }

        public bool MenuOpen
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public double CompactPaneLength
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public string CommandBarClosedDisplayMode
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public SolidColorBrush CommandBarBackgroundColor
        {
            get { return GetStoredProperty<SolidColorBrush>() ?? ClosedPaneColor; }
            set { SetStoredProperty(value); }
        }

        public bool IsCommandBarVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsNavBarVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool CommandBarOpen
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    CommandBarBackgroundColor = (value) ? OpenPaneColor : ClosedPaneColor;

                    if (value)
                    {
                        App.TelemetryService.WriteEvent("CommandBarOpened");
                    }
                }
            }
        }

        public void ToggleFullscreenMode()
        {
            if (_isFullscreen)
            {
                CompactPaneLength = DefaultCompactPaneLength;
                CommandBarClosedDisplayMode = DefaultCommandBarClosedDisplayMode;
                _isFullscreen = false;
            }
            else
            {
                CompactPaneLength = FullscreenCompactPaneLength;
                CommandBarClosedDisplayMode = FullscreenCommandBarClosedDisplayMode;
                MenuOpen = false;
                _isFullscreen = true;
            }
        }

        // ObservableCollection needs to be updated on UI thread
        public ObservableCollection<NavBarDataItem> TopNavBarItems { get; private set; } = new ObservableCollection<NavBarDataItem>
        {
            new NavBarDataItem
            {
                PageName = "Menu",
                Content = Common.GetLocalizedText("NavbarMenuButton"),
                Icon = "\xE700",
            },
            // Start button must be at index 1
            new NavBarDataItem(PageUtil.GetDescriptorFromTypeFullName(typeof(TilePage).FullName)),
            new NavBarDataItem(PageUtil.GetDescriptorFromTypeFullName(typeof(DeviceInfoPage).FullName)),
            new NavBarDataItem(PageUtil.GetDescriptorFromTypeFullName(typeof(CommandLinePage).FullName)),
        };

        // ObservableCollection needs to be updated on UI thread
        public ObservableCollection<NavBarDataItem> BottomNavBarItems { get; } = new ObservableCollection<NavBarDataItem>
        {
            // Sign In button must be at index 0
            new NavBarDataItem(PageUtil.GetDescriptorFromTypeFullName(typeof(AuthenticationPage).FullName)),
            new NavBarDataItem(PageUtil.GetDescriptorFromTypeFullName(typeof(SettingsPage).FullName)),
        };

        public void ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is NavBarDataItem data)
            {
                switch (data.PageName)
                {
                    case "Menu":
                        MenuOpen = !MenuOpen;
                        break;
                    default:
                        if (data.PageName != null)
                        {
                            var descriptor = PageUtil.GetDescriptorFromTypeFullName(data.PageName);
                            if (descriptor != null)
                            {
                                PageService.NavigateTo(descriptor.Type);
                            }
                        }
                        break;
                }

                UpdateSelectedNavBarButton(data.PageName);
            }
        }

        public void UpdateSelectedNavBarButton(string pageTypeFullName)
        {
            InvokeOnUIThread(() =>
            {
                if (pageTypeFullName != null)
                {
                    TopSelectedNavBarItem = TopNavBarItems.FirstOrDefault(x => x.PageName == pageTypeFullName);
                    BottomSelectedNavBarItem = BottomNavBarItems.FirstOrDefault(x => x.PageName == pageTypeFullName);
                }
                else
                {
                    TopSelectedNavBarItem = BottomSelectedNavBarItem = null;
                }
            });
        }

        public void UpdateDefaultPageIcon(string pageTypeFullName)
        {
            var descriptor = PageUtil.GetDescriptorFromTypeFullName(pageTypeFullName);
            InvokeOnUIThread(() =>
            {
                if (TopNavBarItems[1].PageName != typeof(TilePage).FullName)
                {
                    TopNavBarItems.RemoveAt(1);
                }

                if (!string.IsNullOrEmpty(descriptor?.Category))
                {
                    TopNavBarItems.Insert(1, new NavBarDataItem
                    {
                        Content = descriptor.Title,
                        Icon = descriptor.Tag,
                        PageName = descriptor.Type.FullName
                    });
                }
            });
        }

        #endregion

        #region Side Pane

        public double SidePaneWidth
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsSidePaneOpen
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public object SidePaneContent
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }

        public string SidePaneTitle
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public object SidePaneBackground
        {
            get { return GetStoredProperty<object>() ?? OpenPaneColor; }
            set { SetStoredProperty(value); }
        }

        public void ShowSidePane(object content, string title)
        {
            SidePaneTitle = title;
            SidePaneContent = content;
            IsSidePaneOpen = true;
        }

        public void HideSidePane()
        {
            IsSidePaneOpen = false;
            SidePaneContent = null;
            SidePaneTitle = string.Empty;
        }

        #endregion

        #region UI properties and commands

        public double PageWidth
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double PageHeight
        {
            get { return GetStoredProperty<double>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    JumboHeight = 0.36 * value;
                    JumboCanvasTopOffset = (value / 2.0) - (JumboHeight / 2.0);
                }
            }
        }

        public Size PageSize => new Size(PageWidth, PageHeight);

        public double JumboHeight
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double JumboCanvasTopOffset
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public bool ScreensaverEnabled
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public Brush LoadingPanelBackgroundBrush
        {
            get
            {
                return GetStoredProperty<Brush>() ?? 
                    new SolidColorBrush(Colors.Black)
                    {
                        Opacity = 0.9
                    };
            }
            set { SetStoredProperty(value); }
        }

        #endregion

        public void RefreshPageSize()
        {
            // Needs to be run on UI thread because of Window.Current
            InvokeOnUIThread(() => ResizePage(new Size(((Frame)Window.Current.Content).ActualWidth, ((Frame)Window.Current.Content).ActualHeight)));
        }

        public void UpdateScreensaverEnabled()
        {
            ScreensaverEnabled = Settings.ScreensaverEnabled;
        }

        public void PageLoaded(object sender, RoutedEventArgs e)
        {
            RefreshPageSize();
            UpdateScreensaverEnabled();
        }

        public void PageSizeChanged(object sender, WindowSizeChangedEventArgs e)
        {
            ResizePage(e.Size);
        }

        private void ResizePage(Size size)
        {
            double scaling = Settings.AppScaling;
            if (Settings.AppAutoScaling)
            {
                scaling = Math.Max(size.Width / DefaultWidth, 1);
            }

            var adjusted = GetAdjustedDimensions(size, new Size(DefaultWidth * scaling, DefaultWidth * scaling));
            PageWidth = adjusted.Width;
            PageHeight = adjusted.Height;
        }

        public Size GetAdjustedDimensions(Size frameDim, Size targetDim)
        {
            double outputWidth = targetDim.Width;
            double outputHeight = targetDim.Height;

            double targetRatio = frameDim.Width / frameDim.Height;
            outputHeight = outputWidth / targetRatio;
            outputHeight = (double.IsNaN(outputHeight)) ? targetDim.Height : outputHeight;

            return new Size(outputWidth, outputHeight);
        }

        public void SetUpVM()
        {
            RefreshPageSize();

            if (Settings.KioskMode)
            {
                LogService.Write("Kiosk mode enabled, delete the kiosk_mode file in the LocalState folder to disable.");

                CompactPaneLength = 0;
                IsCommandBarVisible = false;
                IsNavBarVisible = false;
            }

            Settings.SettingsUpdated += Settings_SettingsUpdated;

            switch (DeviceTypeInformation.Type)
            {
                case DeviceTypes.RPI2:
                case DeviceTypes.RPI3:
                    LoadingPanelBackgroundBrush = new SolidColorBrush(Colors.Black)
                    {
                        Opacity = 0.9
                    };
                    break;
                default:
                    LoadingPanelBackgroundBrush = new BackdropBlurBrush()
                    {
                        BlurAmount = 8,
                        TintColor = (Color)Application.Current.Resources["LoadingPanelTintColor"]
                    };
                    break;
            }
        }

        public void TearDownVM()
        {
            Settings.SettingsUpdated -= Settings_SettingsUpdated;
        }

        private void Settings_SettingsUpdated(object sender, SettingsUpdatedEventArgs args)
        {
            if (sender is SettingsProvider settings)
            {
                switch (args.Key)
                {
                    case "AppDefaultPage":
                        PageService.UpdateDefaultPageIcon(settings.GetDefaultPageType().FullName);
                        break;
                    case "AppScaling":
                    case "AppAutoScaling":
                        RefreshPageSize();
                        break;
                    case "ScreensaverEnabled":
                        UpdateScreensaverEnabled();
                        break;
                }
            }
        }

        private class SignInStatus
        {
            public SolidColorBrush Brush;
            public string Tag;
            public SignInStatus(SolidColorBrush brush, string tag)
            {
                Brush = brush;
                Tag = tag;
            }
        }

        private SignInStatus[] _signInStatuses = new SignInStatus[]
        {
            // None signed in
            new SignInStatus(new SolidColorBrush(Colors.Red), "\xE8F8"),
            // One signed in
            new SignInStatus(new SolidColorBrush(Colors.Orange), "\xE8F8"),
            // Both signed in
            new SignInStatus(new SolidColorBrush(Colors.Green), "\xE77B"),
        };

        /// <summary>
        /// Sets the state of the MSA Radio button based on whether the user is signed in or not
        /// </summary>
        public void SetSignInStatus(bool msaStatus, bool aadStatus, string name = null)
        {
            bool aadEnabled = AppService.AuthManager.IsAadProviderAvailable();

            if (!aadEnabled)
            {
                aadStatus = msaStatus;
            }

            int signInCount = 0;
            if (msaStatus)
            {
                signInCount++;
            }
            
            if (aadStatus)
            {
                signInCount++;
            }

            var status = _signInStatuses[signInCount];

            InvokeOnUIThread(() =>
            {
                BottomNavBarItems[0].Icon = status.Tag;
                if (aadEnabled)
                {
                    BottomNavBarItems[0].Background = status.Brush;
                }
            });

            SetSignInName(msaStatus, name);
        }

        private void SetSignInName(bool msaStatus, string name)
        {
            InvokeOnUIThread(() =>
            {
                if (!msaStatus)
                {
                    BottomNavBarItems[0].Content = Common.GetLocalizedText("NavbarAuthNoAccount");
                }
                else if (string.IsNullOrEmpty(name))
                {
                    BottomNavBarItems[0].Content = Common.GetLocalizedText("NavbarAuthUnknownAccount");
                }
                else
                {
                    BottomNavBarItems[0].Content = name;
                }
            });
        }
    }
}
