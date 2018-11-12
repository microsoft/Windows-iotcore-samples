// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace SmartDisplay.Views
{
    /// <summary>
    /// This interface handles the navigation events in the OOBE, before
    /// the AppService/PageService are available
    /// </summary>
    public interface IOOBEWindowService
    {
        void Navigate(Type pageType, object parameter = null);

        void ReloadCurrentPage();
    }
}
