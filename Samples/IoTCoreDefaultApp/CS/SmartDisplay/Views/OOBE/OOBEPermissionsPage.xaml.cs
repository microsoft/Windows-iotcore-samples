// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class OOBEPermissionsPage : OOBEPageBase
    {
        public OOBEPermissionsPageVM ViewModel { get; } = new OOBEPermissionsPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public OOBEPermissionsPage()
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
