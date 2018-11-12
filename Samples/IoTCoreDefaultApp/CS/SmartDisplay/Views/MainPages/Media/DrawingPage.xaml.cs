// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Page for drawing things
    /// </summary>
    public sealed partial class DrawingPage : PageBase
    {
        public DrawingPageVM ViewModel { get; } = new DrawingPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public DrawingPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM(inkCanvas);
        }
    }
}
