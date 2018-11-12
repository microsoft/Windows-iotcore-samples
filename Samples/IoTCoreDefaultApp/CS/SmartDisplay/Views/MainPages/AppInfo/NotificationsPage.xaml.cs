// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class NotificationsPage : PageBase
    {
        public NotificationsPageVM ViewModel { get; } = new NotificationsPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public NotificationsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM();
        }
    }
}
