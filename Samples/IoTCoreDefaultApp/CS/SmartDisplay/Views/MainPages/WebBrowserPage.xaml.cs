// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Page for WebBrowser
    /// </summary>
    public sealed partial class WebBrowserPage : PageBase, IWebViewElementController
    {
        public WebBrowserPageVM ViewModel { get; } = new WebBrowserPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        private ILogService LogService => AppService.LogService;

        private static TimeSpan _longRunningTime = new TimeSpan(0, 0, 30);

        private List<string> _suggestedUrls = new List<string>
        {
            Constants.WODUrl,
            Constants.IoTHacksterUrl,
            Constants.IoTGitHubUrl,
        };

        public WebBrowserPage()
        {
            DataContext = LanguageManager.GetInstance();
            InitializeComponent();

            WebAddress.QueryIcon.Arrange(new Rect(5, 5, 10, 10));
            WebAddress.ItemsSource = _suggestedUrls;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM(this, e.Parameter as string);
            PopulateCommandBar();
        }

        private void PopulateCommandBar()
        {
            PageService?.AddCommandBarButton(CommandBarButton.Separator);
            PageService?.AddCommandBarButton(PageUtil.CreatePageSettingCommandBarButton(
                PageService,
                new BrowserSettingsControl
                {
                    Width = Constants.DefaultSidePaneContentWidth,
                    Background = new SolidColorBrush(Colors.Transparent),
                },
                Common.GetLocalizedText("BrowserSettingHeader/Text")));
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            WebAddress.Focus(FocusState.Pointer);
        }

        #region IWebViewElementController

        void IWebViewElementController.Navigate(Uri uri) => WebView.Navigate(uri);
        bool IWebViewElementController.CanGoBack => WebView.CanGoBack;
        void IWebViewElementController.GoBack() => WebView.GoBack();
        bool IWebViewElementController.CanGoForward => WebView.CanGoForward;
        void IWebViewElementController.GoForward() => WebView.GoForward();
        void IWebViewElementController.Stop() => WebView.Stop();

        #endregion

        #region Address bar event handlers

        private void WebAddress_FocusEngaged(Control sender, FocusEngagedEventArgs args)
        {
            WebAddress.IsSuggestionListOpen = true;
        }

        private void WebAddress_LostFocus(object sender, RoutedEventArgs e)
        {
            ViewModel.TargetUrl = ViewModel.CurrentUrl;
        }

        private void WebAddress_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            WebAddress.IsSuggestionListOpen = true;
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                ViewModel.Browse();
            }
        }

        private void WebAddress_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.QueryText.Length > 0)
            {
                if (args.ChosenSuggestion != null)
                {
                    ViewModel.TargetUrl = args.QueryText;
                }
                ViewModel.Browse();
            }
        }

        private void WebAddress_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            ViewModel.TargetUrl = args.SelectedItem.ToString();
        }

        private void WebAddress_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // End InProgress when the user edits the URL
            ViewModel.InProgress = false;
        }

        #endregion

        #region WebView event handlers

        private void WebView_ContainsFullScreenElementChanged(WebView sender, object args)
        {
            var applicationView = ApplicationView.GetForCurrentView();

            if (sender.ContainsFullScreenElement)
            {
                applicationView.TryEnterFullScreenMode();
            }
            else if (applicationView.IsFullScreenMode)
            {
                applicationView.ExitFullScreenMode();
            }
        }

        private void WebView_FrameDOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            LogService.Write(args.Uri.ToString());
        }

        private void WebView_DOMContentLoaded(WebView sender, WebViewDOMContentLoadedEventArgs args)
        {
            LogService.Write(args.Uri.ToString());
        }

        private void WebView_NavigationStarting(object sender, WebViewNavigationStartingEventArgs args)
        {
            LogService.Write(args.Uri.ToString());

            if (ViewModel.IsNonUri(args.Uri.AbsoluteUri.ToString()))
            {
                ViewModel.InProgress = false;
            }
            else if (!ViewModel.IsAllowedUri(args.Uri))
            {
                // Cancel navigation if URL is not allowed.
                ViewModel.InProgress = false;
                args.Cancel = true;
            }
            else
            {
                ViewModel.InProgress = true;
                WebAddress.IsSuggestionListOpen = false;
            }
        }

        private void WebView_NavigationCompleted(WebView sender, WebViewNavigationCompletedEventArgs args)
        {
            LogService.Write(args.Uri.ToString());

            ViewModel.InProgress = false;

            if (args.IsSuccess)
            {
                ViewModel.OnWebViewNavigationSuccess(args.Uri.ToString());
            }
            else
            {
                ViewModel.DisplayMessage(string.Format(
                    Common.GetLocalizedText("WebNavigationErrorFormat"),
                    ((int)args.WebErrorStatus).ToString(),
                    args.WebErrorStatus.ToString()));
            }
        }

        private void WebView_NewWindowRequested(WebView sender, WebViewNewWindowRequestedEventArgs e)
        {
            ViewModel.BrowseTo(e.Uri.ToString(), true);
        }

        private void WebView_LongRunningScriptDetected(WebView sender, WebViewLongRunningScriptDetectedEventArgs args)
        {
            LogService.Write();

            // Halt script running more than 30 secs 
            if (args.ExecutionTime > _longRunningTime)
            {
                args.StopPageScriptExecution = true;
                ViewModel.InProgress = false;
            }
        }

        private async void WebView_UnviewableContentIdentified(WebView sender, WebViewUnviewableContentIdentifiedEventArgs args)
        {
            // This URI can't be handled by the WebView control. Launch the default system handler for it.
            await Windows.System.Launcher.LaunchUriAsync(args.Uri);
        }

        #endregion
    }
}
