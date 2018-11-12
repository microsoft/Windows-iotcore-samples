// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class HelpPage : PageBase
    {
        public HelpPageVM ViewModel { get; } = new HelpPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public HelpPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is string helpText)
            {
                ViewModel.SetUpVM(helpText);
            }
        }
    }
}
