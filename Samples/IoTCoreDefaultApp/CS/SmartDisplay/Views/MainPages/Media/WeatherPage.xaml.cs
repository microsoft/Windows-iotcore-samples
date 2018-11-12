// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Displays weather for your location
    /// </summary>
    public sealed partial class WeatherPage : PageBase
    {
        public WeatherPageVM ViewModel { get; } = new WeatherPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public WeatherPage()
        {
            InitializeComponent();
            DataContext = ViewModel;
        }
        
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.TearDownVM();
        }
    }
}
