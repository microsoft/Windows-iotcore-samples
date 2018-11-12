// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Views;
using System;
using System.Linq;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public class OOBEPageBase : PageBase, IOOBEWindowService
    {
        #region IOOBEWindowService

        public void Navigate(Type pageType, object parameter = null)
        {
            var unused = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (Window.Current.Content is Frame frame)
                {
                    frame.Navigate(pageType, parameter);
                }
            });
        }

        public void ReloadCurrentPage()
        {
            var unused = Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (Window.Current.Content is Frame frame)
                {
                    frame.Navigate(frame.CurrentSourcePageType);
                    frame.BackStack.Remove(frame.BackStack.Last());
                }
            });
        }

        #endregion
    }
}
