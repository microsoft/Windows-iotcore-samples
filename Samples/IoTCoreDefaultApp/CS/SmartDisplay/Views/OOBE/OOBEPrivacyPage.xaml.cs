// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class OOBEPrivacyPage : OOBEPageBase
    {
        public OOBEPrivacyPageVM ViewModel { get; } = new OOBEPrivacyPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;
        public OOBEPrivacyPage()
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
