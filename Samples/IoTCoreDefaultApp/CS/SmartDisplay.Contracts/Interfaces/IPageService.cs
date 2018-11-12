// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using System;
using System.Collections.ObjectModel;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Contracts
{
    public interface IPageService
    {
        Size GetContentFrameDimensions();

        ICommandBarElement AddCommandBarButton(CommandBarButton button);

        ICommandBarElement AddSecondaryCommandBarButton(CommandBarButton button);

        void AddDevicePortalButtons(Type redirect);

        void NavigateToHome();

        void NavigateToHelp(string helpText);

        void NavigateTo(Type page, object parameter = null, bool reload = false);

        void SetSignInStatus(bool msaStatus, bool aadStatus, string name = null);

        void UpdateDefaultPageIcon(string pageType);

        void ShowLoadingPanel(string loadingText = null);

        void HideLoadingPanel();

        ObservableCollection<Notification> NotificationHistory { get; }
        
        void ShowNotification(string text, int timeoutMilliseconds = 3000, string symbol = "🔵", Action clickHandler = null);

        void ShowJumboNotification(string text, Color color, int timeoutMilliseconds = 5000, string symbol = "🔴");

        void ShowJumboNotificationWithImage(string text, Color color, int timeoutMilliseconds = 5000, StorageFile symbolFile = null);

        Type GetPageTypeByFullName(string fullName);

        void ShowSidePane(object content, string title = null);

        void HideSidePane();
    }
}
