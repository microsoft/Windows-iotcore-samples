// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class OOBEWelcomePage : OOBEPageBase
    {
        public OOBEWelcomePageVM ViewModel { get; } = new OOBEWelcomePageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;
        public OOBEWelcomePage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM(this);
        }
    }
}
