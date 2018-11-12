// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Identity;
using SmartDisplay.Utils;
using SmartDisplay.Utils.UI;
using SmartDisplay.ViewModels;
using SmartDisplay.Views;
using SmartDisplay.Views.MainPages;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay
{
    /// <summary>
    /// Main page that holds the persistent navigation bar and frame for the various views
    /// </summary>
    public partial class MainPage : PageBase, IPageService, IDisposable
    {
        private MainPageVM ViewModel { get; } = new MainPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        private UiManager UiManager { get; set; }

        private ICustomContentService CustomContentService => AppService.GetRegisteredService<ICustomContentService>();
                
        #region Initialization

        public MainPage()
        {
            InitializeComponent();

            // Create the UiManager to coordinate popup UI
            UiManager = UiManager.Create(Dispatcher, _notification);

            // Set this MainPage as the IPageService provider for the IAppService for this dispatcher.
            ((AppService)AppService).SetPageService(this);

            SubscribeToEvents();

            // Load default page icon            
            UpdateDefaultPageIcon(App.Settings.AppDefaultPage);
        }

        private void SubscribeToEvents()
        {
            // Setup content frame
            _contentFrame.Navigated += ContentFrame_Navigated;
            _contentFrame.Navigating += ContentFrame_Navigating;
            _contentFrame.NavigationStopped += ContentFrame_NavigationStopped;
            _contentFrame.NavigationFailed += ContentFrame_NavigationFailed;

            // These two events are for resizing the Viewbox contents so that it fills the window and doesn't leave black bars
            Window.Current.SizeChanged += ViewModel.PageSizeChanged;
            Loaded += ViewModel.PageLoaded;
        }

        private void UnsubscribeToEvents()
        {
            // Setup content frame
            _contentFrame.Navigated -= ContentFrame_Navigated;
            _contentFrame.Navigating -= ContentFrame_Navigating;
            _contentFrame.NavigationStopped -= ContentFrame_NavigationStopped;
            _contentFrame.NavigationFailed -= ContentFrame_NavigationFailed;

            // These two events are for resizing the Viewbox contents so that it fills the window and doesn't leave black bars
            Window.Current.SizeChanged -= ViewModel.PageSizeChanged;
            Loaded -= ViewModel.PageLoaded;
        }

        private async Task RefreshAuthAsync()
        {
            var aadProvider = App.AuthManager.GetGraphProvider();
            var msaProvider = App.AuthManager.GetProvider(ProviderNames.MsaProviderKey);

            // Try to get tokens using saved account info
            bool aadStatus = (aadProvider != null) ? await aadProvider.GetTokenSilentAsync() != null : true;
            bool msaStatus = await msaProvider.GetTokenSilentAsync() != null;

            ViewModel.SetSignInStatus(msaStatus, aadStatus);

            // Check for changes periodically. 
            // When change is detected, TokenStatusChanged event is fired
            App.AuthManager.StartStatusTimer(3600 * 12);
        }

        #endregion

        #region IPageService

        /// <summary>
        /// Gets the size of the content frame so that the pages inside of the frame can be resized to fill the frame
        /// </summary>
        /// <returns>The size of the content frame</returns>
        public Size GetContentFrameDimensions() => new Size(_contentFrame.ActualWidth, _contentFrame.ActualHeight);

        /// <summary>
        /// Adds a button to the primary CommandBar button list
        /// </summary>
        /// <param name="button"></param>
        /// <returns>The newly created AppBarButton</returns>
        public ICommandBarElement AddCommandBarButton(CommandBarButton button) => AddCommandBarButton(_commandBar.PrimaryCommands, button);

        /// <summary>
        /// Adds a button to the secondary CommandBar button list
        /// </summary>
        /// <param name="button"></param>
        /// <returns>The newly created AppBarButton</returns>
        public ICommandBarElement AddSecondaryCommandBarButton(CommandBarButton button) => AddCommandBarButton(_commandBar.SecondaryCommands, button);

        private ICommandBarElement AddCommandBarButton(IObservableVector<ICommandBarElement> commands, CommandBarButton button)
        {
            if (button == CommandBarButton.Separator)
            {
                var separator = new AppBarSeparator();
                commands.Add(separator);
                return separator;
            }

            var appBarButton = new AppBarButton()
            {
                Icon = button.Icon,
                Label = button.Label,
            };
            appBarButton.Click += button.Handler;
            commands.Add(appBarButton);
            return appBarButton;
        }

        /// <summary>
        /// Adds additional buttons to the CommandBar for interacting with Windows Device Portal
        /// </summary>
        public void AddDevicePortalButtons(Type sourcePage)
        {
            AddCommandBarButton(CommandBarButton.Separator);

            AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Clear),
                Label = Common.GetLocalizedText("ClearWDPCredentialsButton"),
                Handler = (s, e) =>
                {
                    DevicePortalUtil.ClearCredentials();
                    ShowNotification(Common.GetLocalizedText("ClearedWDPCredentialsText"));
                    NavigateTo(sourcePage, reload: true);
                }
            });
        }

        /// <summary>
        /// Opens the home page stored in app settings
        /// </summary>
        public void NavigateToHome()
        {
            // Navigate to default page
            Type pageType = App.Settings.GetDefaultPageType();
            NavigateTo(pageType, reload: true);
        }

        public void NavigateToHelp(string helpText) => NavigateTo(typeof(HelpPage), helpText);

        /// <summary>
        /// Displays a page of type <paramref name="page"/> in the content frame
        /// </summary>
        /// <param name="page"></param>
        /// <param name="parameter"></param>
        /// <param name="reload"></param>
        public async void NavigateTo(Type page, object parameter = null, bool reload = false)
        {
            App.LogService.Write($"Name: {page.Name}, Full Name: {page.FullName}");

            if (_contentFrame.CurrentSourcePageType != page || reload)
            {
                if (SettingsPage.SettingsPages.ContainsValue(page))
                {
                    parameter = SettingsPage.SettingsPages.FirstOrDefault(x => x.Value == page).Key;
                    page = typeof(SettingsPage);
                }

                // Sometimes if a page is in the middle of navigating, Navigate() will fail, so retry
                for (int i = 0; i < 3 && !_contentFrame.Navigate(page, parameter); i++)
                {
                    App.LogService.Write($"Failed to navigate to {page.Name}, trying again...", Windows.Foundation.Diagnostics.LoggingLevel.Warning);
                    await Task.Delay(200);
                }
            }
        }

        /// <summary>
        /// Updates the sign-in status, including the sign-in button color, icon, and text
        /// </summary>
        /// <param name="msaStatus"></param>
        /// <param name="aadStatus"></param>
        /// <param name="name"></param>
        public void SetSignInStatus(bool msaStatus, bool aadStatus, string name) => ViewModel?.SetSignInStatus(msaStatus, aadStatus, name);

        /// <summary>
        /// Sets the "default page" button to the page type in <paramref name="pageTypeFullName"/>
        /// </summary>
        /// <param name="pageTypeFullName"></param>
        public void UpdateDefaultPageIcon(string pageTypeFullName) => ViewModel?.UpdateDefaultPageIcon(pageTypeFullName);

        /// <summary>
        /// Displays a semi-opaque panel over the content frame
        /// </summary>
        /// <param name="loadingText"></param>
        public void ShowLoadingPanel(string loadingText) => _loadingPanel.Show(loadingText);

        /// <summary>
        /// Hides the panel that was shown with <see cref="ShowLoadingPanel"/>
        /// </summary>
        public void HideLoadingPanel() => _loadingPanel.Hide();

        /// <summary>
        /// A list of recent notifications shown on this page
        /// </summary>
        public ObservableCollection<Notification> NotificationHistory => UiManager?.NotificationHistory;

        /// <summary>
        /// Displays an in-app notification
        /// </summary>
        /// <param name="text"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="symbol"></param>
        public async void ShowNotification(string text, int timeoutMilliseconds = 3000, string symbol = "🔵", Action clickHandler = null)
        {
            await UiManager.ShowNotificationAsync(text, timeoutMilliseconds, symbol, clickHandler);
        }

        /// <summary>
        /// Displays a large notification
        /// </summary>
        /// <param name="text"></param>
        /// <param name="color"></param>
        /// <param name="timeoutMilliseconds"></param>
        /// <param name="symbol"></param>
        public async void ShowJumboNotification(string text, Color color, int timeoutMilliseconds = 5000, string symbol = "🔴")
        {
            await UiManager.RunExclusiveTaskAsync(() => ShowJumboNotificationAsync(text, color, timeoutMilliseconds, symbol));
        }

        public async void ShowJumboNotificationWithImage(string text, Color color, int timeoutMilliseconds = 5000, StorageFile symbolFile = null)
        {
            await UiManager.RunExclusiveTaskAsync(() => ShowJumboNotificationAsync(text, color, timeoutMilliseconds, symbolFile));
        }

        private async Task ShowJumboNotificationAsync(string text, Color color, int timeoutMilliseconds = 5000, string symbol = "🔴")
        {
            App.LogService.Write($"Showing jumbo notification: {text}");

            // Record notification
            UiManager.AddNotificationToHistory(new Notification()
            {
                Text = text,
                Timestamp = DateTime.Now,
                Symbol = symbol
            });

            await _jumboNotification.ShowAsync(text, color, timeoutMilliseconds, symbol);
        }

        private async Task ShowJumboNotificationAsync(string text, Color color, int timeoutMilliseconds = 5000, StorageFile symbolFile = null)
        {
            App.LogService.Write($"Showing jumbo notification: {text}");

            // Record notification
            UiManager.AddNotificationToHistory(new Notification()
            {
                Text = text,
                Timestamp = DateTime.Now,
                Symbol = JumboNotificationControl.DefaultSymbol
            });

            await _jumboNotification.ShowWithImageAsync(text, color, timeoutMilliseconds, symbolFile);
        }

        public Type GetPageTypeByFullName(string name)
        {
            return Type.GetType(name);
        }

        public void ShowSidePane(object content, string title = null) => ViewModel.ShowSidePane(content, title);

        public void HideSidePane() => ViewModel.HideSidePane();

        #endregion

        #region Navigation

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            if (!ApplicationData.Current.LocalSettings.Values.ContainsKey(Constants.HasDoneOOBEKey))
            {
                ApplicationData.Current.LocalSettings.Values[Constants.HasDoneOOBEKey] = Constants.HasDoneOOBEValue;

                try
                {
                    var logoFile = await Package.Current.InstalledLocation.GetFileAsync(@"Assets\logo_white.png");

                    ShowJumboNotificationWithImage(Common.GetLocalizedText("WelcomeMessage"),
                        JumboNotificationControl.DefaultColor,
                        JumboNotificationControl.DefaultTimeoutMs,
                        logoFile);
                }
                catch (Exception ex)
                {
                    AppService.LogService.WriteException(ex);
                }
            }

            base.OnNavigatedTo(e);

            ViewModel.SetUpVM();

            // Initialize MSA/AAD
            var refreshAuthTask = RefreshAuthAsync();

            if (e.Parameter is Type pageType)
            {
                NavigateTo(pageType);
            }
            else if (e.Parameter is Tuple<ViewLifetimeControl, object> paramTuple)
            {
                var viewControl = paramTuple.Item1;
                viewControl.Released += ViewControl_Released;

                // A page for the content frame was specified
                if (paramTuple.Item2 is Type nestedPageType)
                {
                    NavigateTo(nestedPageType);
                }
            }
            else if (e.Parameter is Tuple<Type, string> webPageTuple)
            {
                NavigateTo(webPageTuple.Item1, webPageTuple.Item2);
            }
            else
            {
                NavigateToHome();
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.TearDownVM();
        }

        private void ViewControl_Released(object sender, EventArgs e)
        {
            if (sender is ViewLifetimeControl viewControl)
            {
                viewControl.Released -= ViewControl_Released;

                // Each page should clean up any event handlers in OnNavigatedFrom()
                // Clean up the content frame's event handlers by navigating away to a blank Page
                NavigateTo(typeof(PageBase));

                UnsubscribeToEvents();

                // Tell the AppService that this page no longer provides page services.
                ((AppService)AppService).SetPageService(null);

                // Clean up disposable resources
                Dispose();

                // The released event is fired on the thread of the window
                // it pertains to.
                //
                // It's important to make sure no work is scheduled on this thread
                // after it starts to close (no data binding changes, no changes to
                // XAML, creating new objects in destructors, etc.) since
                // that will throw exceptions
                Window.Current.Close();

                App.LogService.Write("Window closed.");
            }
        }

        private void ContentFrame_Navigated(object sender, NavigationEventArgs e)
        {
            ViewModel.MenuOpen = false;
            ViewModel.UpdateSelectedNavBarButton(e.SourcePageType.FullName);

            ResetCommandBar();

            App.LogService.Write(e.SourcePageType.Name);
            App.TelemetryService.WriteEvent("Navigated", new
            {
                pageName = e.SourcePageType.Name
            });
        }

        private void ContentFrame_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            App.LogService.Write(e.SourcePageType.Name);
        }

        private void ContentFrame_NavigationFailed(object sender, NavigationFailedEventArgs e)
        {
            App.LogService.Write(e.Exception.ToString());
        }

        private void ContentFrame_NavigationStopped(object sender, NavigationEventArgs e)
        {
            App.LogService.Write(e.SourcePageType.Name);
        }

        #endregion

        #region Command Bar

        private void ResetCommandBar()
        {
            _commandBar.PrimaryCommands.Clear();
            _commandBar.SecondaryCommands.Clear();

            PopulateCommandBar();
        }

        private void PopulateCommandBar()
        {
            UpdateNavButtons();

            AddCommandBarButton(new CommandBarButton
            {
                Icon = new FontIcon
                {
                    Glyph = "\uE946"
                },
                Label = Common.GetLocalizedText("CommandBarDeviceInfoButton"),
                Handler = DisplayDeviceInfoDialog,
            });

            AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.FullScreen),
                Label = Common.GetLocalizedText("CommandBarFullscreenButton"),
                Handler = (s, e) => ViewModel.ToggleFullscreenMode(),
            });

            if (App.MultiViewManager.IsMultiViewAvailable())
            {
                AddCommandBarButton(new CommandBarButton
                {
                    Icon = new SymbolIcon(Symbol.NewWindow),
                    Label = Common.GetLocalizedText("CommandBarMultiviewButton"),
                    Handler = async (s, e) =>
                    {
                        await App.MultiViewManager.CreateAndShowViewAsync(
                            typeof(MainPage),
                            _contentFrame.CurrentSourcePageType);
                    },
                });
            }

            string pageFullName = _contentFrame.CurrentSourcePageType?.FullName;
            var descriptor = PageUtil.GetDescriptorFromTypeFullName(pageFullName);
            // Check if page is eligible for "Set Default" action
            if (descriptor != null &&
                App.Settings.AppDefaultPage != pageFullName)
            {
                AddSecondaryCommandBarButton(new CommandBarButton
                {
                    Icon = new SymbolIcon(Symbol.Home),
                    Label = Common.GetLocalizedText("CommandBarSetDefaultButton"),
                    Handler = (s, e) =>
                    {
                        var pageTypeFullName = _contentFrame.CurrentSourcePageType.FullName;
                        App.Settings.AppDefaultPage = pageTypeFullName;
                        ViewModel.UpdateSelectedNavBarButton(pageTypeFullName);

                        ShowNotification(string.Format(Common.GetLocalizedText("SetDefaultPageSuccessFormat"), descriptor.Title), symbol: descriptor.Icon);
                    },
                });
            }

            AddSecondaryCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Save),
                Label = Common.GetLocalizedText("CommandBarSaveLogButton"),
                Handler = SaveLogButton_Click,
            });

            AddSecondaryCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Document),
                Label = Common.GetLocalizedText("CommandBarSessionLogButton"),
                Handler = (s, e) =>
                {
                    NavigateTo(typeof(SessionLogPage));
                },
            });
        }

        private AppBarButton _backButton;
        private AppBarButton _forwardButton;
        private void UpdateNavButtons()
        {
            if (_commandBar.Content == null)
            {
                _backButton = new AppBarButton()
                {
                    Icon = new SymbolIcon(Symbol.Back),
                    Label = Common.GetLocalizedText("CommandBarBackButton"),
                    LabelPosition = (_commandBar.IsOpen) ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed,
                };
                _backButton.Click += NavigateBack;

                _forwardButton = new AppBarButton()
                {
                    Icon = new SymbolIcon(Symbol.Forward),
                    Label = Common.GetLocalizedText("CommandBarForwardButton"),
                    LabelPosition = (_commandBar.IsOpen) ? CommandBarLabelPosition.Default : CommandBarLabelPosition.Collapsed,
                };
                _forwardButton.Click += NavigateForward;

                var panel = new StackPanel();
                panel.Orientation = Orientation.Horizontal;
                panel.Children.Add(_backButton);
                panel.Children.Add(_forwardButton);
                _commandBar.Content = panel;

                // Show/hide labels on command bar open/close
                _commandBar.Opening += (s, e) =>
                {
                    _backButton.LabelPosition = _forwardButton.LabelPosition = CommandBarLabelPosition.Default;
                };

                _commandBar.Closing += (s, e) =>
                {
                    _backButton.LabelPosition = _forwardButton.LabelPosition = CommandBarLabelPosition.Collapsed;
                };
            }
            _backButton.IsEnabled = _contentFrame.CanGoBack;
            _forwardButton.IsEnabled = _contentFrame.CanGoForward;
        }

        public void NavigateForward(object sender, RoutedEventArgs e)
        {
            if (_contentFrame.CanGoForward)
            {
                _contentFrame.GoForward();
            }
        }

        public void NavigateBack(object sender, RoutedEventArgs e)
        {
            if (_contentFrame.CanGoBack)
            {
                _contentFrame.GoBack();
            }
        }

        // Displays basic device info
        private void DisplayDeviceInfoDialog(object sender, RoutedEventArgs e)
        {
            string msg = string.Format(Common.GetLocalizedText("DeviceInfoDialogMessage"),
                NetworkPresenter.GetCurrentIpv4Address(),
                Common.GetOSVersionString(),
                Common.GetAppVersion());

            foreach (var feature in AppComposer.Imports.Features)
            {
                if (!string.IsNullOrEmpty(feature.DeviceInfo))
                {
                    msg += "\n" + feature.DeviceInfo;
                }
            }

            DialogButton primaryButton = new DialogButton(Common.GetLocalizedText("DeviceInfoDialogButton"), (Sender, ClickEventsArgs) =>
            {
                NavigateTo(typeof(DeviceInfoPage));
            });

            AppService.DisplayDialog(Common.GetLocalizedText("DeviceInfoDialogTitle"), msg, primaryButton);
        }

        private async void SaveLogButton_Click(object sender, RoutedEventArgs e)
        {
            var logFile = await App.LogService.SaveLogToFileAsync(Constants.EtwProviderName);
            ShowNotification(string.Format(Common.GetLocalizedText("LogSavedText"), logFile.Name), 10000, clickHandler: () => PageService.NavigateTo(typeof(LogsPage)));

            var provider = App.AuthManager.GetGraphProvider();
            if (provider == null)
            {
                return;
            }

            if (provider.IsTokenValid())
            {
                using (var graphHelper = new GraphHelper(provider))
                {
                    if (await AppService.YesNoAsync(Common.GetLocalizedText("EmailLogDalogTitle"), string.Format(Common.GetLocalizedText("EmailLogDialogMessage"), logFile.Name)))
                    {
                        string messageContent = LogUtil.CreateMessageContent(
                            _contentFrame.CurrentSourcePageType.Name,
                            CustomContentService?.GetContent<string>(CustomContentConstants.BugTemplate));

                        try
                        {
                            var email = await LogUtil.EmailLogsAsync(graphHelper, "[Smart Display] LOG MAILER", messageContent, new StorageFile[] { logFile });
                            ShowNotification(string.Format(Common.GetLocalizedText("LogMailedText"), email));
                        }
                        catch (Exception ex)
                        {
                            ShowNotification(string.Format(Common.GetLocalizedText("EmailLogErrorText"), ex.Message));
                        }
                    }
                    else
                    {
                        ShowNotification(Common.GetLocalizedText("EmailLogNoMessage"), clickHandler: () => PageService.NavigateTo(typeof(LogsPage)));
                    }
                }
            }
        }

        #endregion

        #region IDisposable
        bool _disposed;
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources here.
                    UiManager.Dispose();
                }

                // Dispose unmanaged resources here.
                _disposed = true;
            }
        }

        ~MainPage()
        {
            Dispose(false);
        }
        #endregion
    }
}
