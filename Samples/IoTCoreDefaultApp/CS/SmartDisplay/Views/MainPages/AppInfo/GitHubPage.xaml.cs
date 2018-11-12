// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;

namespace SmartDisplay.Views
{
    /// <summary>
    /// This page is a simple wrapper around the browser page.  This needs to be its own page 
    /// so that it can be set as a default page
    /// </summary>
    public sealed partial class GitHubPage : PageBase
    {
        public GitHubPage()
        {
            InitializeComponent();
            ContentFrame.Navigate(typeof(WebBrowserPage), UrlConstants.SmartDisplayGitHubURL);
        }
    }
}
