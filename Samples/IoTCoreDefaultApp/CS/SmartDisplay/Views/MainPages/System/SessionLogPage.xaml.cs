// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.MainPages
{
    public sealed partial class SessionLogPage : PageBase
    {
        public SessionLogPageVM ViewModel { get; } = new SessionLogPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public SessionLogPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}
