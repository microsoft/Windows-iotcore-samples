// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Logging;
using SmartDisplay.Utils;
using SmartDisplay.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace SmartDisplay.ViewModels
{
    public class WebBrowserPageVM : BaseViewModel
    {
        #region UI properties and commands

        // TargetURL is the text that is shown in the web address bar
        public string TargetUrl
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public bool InProgress
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    NotifyPropertyChanged("RefreshButtonContent");
                }
            }
        }

        public string RefreshButtonContent => InProgress ? "\xE711" : "\xE72C";

        public bool CanGoBack
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool CanGoForward
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool IsMessageVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string MessageLine1
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string MessageLine2
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        #region Commands

        private RelayCommand _actionCommand;
        public ICommand ActionCommand
        {
            get
            {
                return _actionCommand ??
                    (_actionCommand = new RelayCommand((object parameter) =>
                    {
                        if (parameter is string action)
                        {
                            switch (action)
                            {
                                case "NavigateBack":
                                    if (CanGoBack)
                                    {
                                        InProgress = true;
                                        WebViewController.GoBack();
                                    }
                                    break;

                                case "NavigateForward":
                                    if (CanGoForward)
                                    {
                                        InProgress = true;
                                        WebViewController.GoForward();
                                    }
                                    break;

                                case "Refresh":
                                    if (!InProgress)
                                    {
                                        Browse();
                                    }
                                    else
                                    {
                                        // If navigation is in progress then the button changes to Stop.
                                        InProgress = false;
                                        WebViewController.Stop();
                                    }
                                    break;

                                case "NavigateHome":
                                    BrowseTo(App.Settings.BrowserHomePage, true);
                                    break;

                                case "NetworkPreferences":
                                    PageService?.NavigateTo(typeof(SettingsPage), Common.GetLocalizedText("NetworkPreferences/Text"));
                                    break;
                            }
                        }
                    }));
            }
        }

        #endregion

        // CurrentUrl stores the last successful URL we loaded
        public string CurrentUrl { get; set; } = string.Empty;

        private IWebViewElementController WebViewController { get; set; }

        public WebBrowserPageVM() : base()
        {
        }

        internal void SetUpVM(IWebViewElementController controller, string initialUrl)
        {
            WebViewController = controller;

            if (!string.IsNullOrWhiteSpace(initialUrl))
            {
                BrowseTo(initialUrl);
            }
            else if (string.IsNullOrWhiteSpace(CurrentUrl))
            {
                BrowseTo(App.Settings.BrowserHomePage);
            }
        }


        /// <summary>
        /// Returns true if we should allow navigation to this URL.
        /// </summary>
        public bool IsAllowedUri(Uri url)
        {
            // TODO: Add your own URL filtering code here.
            return true;
        }

        /// <summary>
        /// Returns true if the string is not a valid URL.
        /// </summary>
        public bool IsNonUri(string uriText)
        {
            if (string.IsNullOrWhiteSpace(uriText) ||
                uriText.ToLower().Equals("about:blank") ||
                uriText.ToLower().StartsWith("javascript:void"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void DisplayMessage(string message1, string message2 = null)
        {
            MessageLine1 = message1 ?? string.Empty;
            MessageLine2 = message2 ?? string.Empty;
            IsMessageVisible = true;
        }

        public void DismissMessage()
        {
            IsMessageVisible = false;
        }

        /// <summary>
        /// Navigates to the URL that is currently in the web address bar
        /// </summary>
        public void Browse()
        {
            BrowseTo(TargetUrl, forceRefresh: true);
        }

        /// <summary>
        /// Navigates to a specific URL
        /// </summary>
        public void BrowseTo(string url, bool forceRefresh = false)
        {
            TargetUrl = url.Trim();

            if (TargetUrl.Length == 0)
            {
                return;
            }

            // Skip the initial load if the user is navigating to WebBrowserPage a second time
            if (!forceRefresh && TargetUrl.Equals(CurrentUrl))
            {
                return;
            }

            InProgress = true;
            DismissMessage();

            try
            {
                // Default to http
                if (!(url.StartsWith("http://") || url.StartsWith("https://")))
                {
                    url = "http://" + url;
                }

                if (!Uri.IsWellFormedUriString(url, UriKind.Absolute))
                {
                    // Enable Search 
                    url = string.Format("http://{0}/search?q={1}", Constants.BingHomePageUrl, TargetUrl);
                }

                // Navigate
                WebViewController.Navigate(new Uri(url));

                App.TelemetryService.WriteEvent("WebBrowserGoButtonClicked");
            }
            catch (Exception)
            {
                TargetUrl = Constants.BingHomePageUrl;
            }
        }

        public void OnWebViewNavigationSuccess(string uri)
        {
            TargetUrl = uri.Trim();
            CurrentUrl = TargetUrl;

            InProgress = false;
            CanGoBack = WebViewController.CanGoBack;
            CanGoForward = WebViewController.CanGoForward;
        }
    }
}
