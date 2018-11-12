// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace SmartDisplay.ViewModels
{
    /// <summary>
    /// Interface used when communicating between a view and view model to control a WebView instance
    /// </summary>
    internal interface IWebViewElementController
    {
        /// <summary>
        /// Navigates to a URI
        /// </summary>
        void Navigate(Uri uri);

        /// <summary>
        /// Cancels any current page navigation or download
        /// </summary>
        void Stop();

        /// <summary>
        /// Returns true if there is at least one page in the backward navigation history
        /// </summary>
        bool CanGoBack { get; }

        /// <summary>
        /// Navigates to the previous page in the navigation history
        /// </summary>
        void GoBack();

        /// <summary>
        /// Returns true if there is at least one page in the forward navigation history
        /// </summary>
        bool CanGoForward { get; }

        /// <summary>
        /// Navigates to the next page in the navigation history
        /// </summary>
        void GoForward();
    }
}
