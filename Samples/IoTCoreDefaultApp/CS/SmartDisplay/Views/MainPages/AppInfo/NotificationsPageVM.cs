// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.UI.Xaml.Controls;
using SmartDisplay.Utils;

namespace SmartDisplay.ViewModels
{
    public class NotificationsPageVM : BaseViewModel
    {
        #region UI properties

        public ObservableCollection<Notification> NotificationCollection
        {
            get { return GetStoredProperty<ObservableCollection<Notification>>(); }
            set { SetStoredProperty(value); }
        }

        #endregion

        public NotificationsPageVM() : base()
        {
            NotificationCollection = new ObservableCollection<Notification>();
        }

        public void SetUpVM()
        {
            NotificationCollection = PageService.NotificationHistory;

            if (Debugger.IsAttached)
            {
                PopulateCommandBar();
            }
        }

        private void PopulateCommandBar()
        {
            PageService.AddCommandBarButton(CommandBarButton.Separator);
            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Up),
                Label = Common.GetLocalizedText("TestNotificationButtonText"),
                Handler = (s, e) => PageService.ShowNotification(Common.GetLocalizedText("TestNotificationMessage")),
            });
            PageService.AddCommandBarButton(new CommandBarButton
            {
                Icon = new SymbolIcon(Symbol.Up),
                Label = Common.GetLocalizedText("TestJumboNotificationButtonText"),
                Handler = (s, e) => PageService.ShowJumboNotification(
                    Common.GetLocalizedText("WelcomeMessage"),
                    JumboNotificationControl.DefaultColor,
                    5000,
                    JumboNotificationControl.DefaultSymbol),
            });
        }
    }
}
