// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class WeatherPage : PageBase
    {
        private WeatherPageVM ViewModel { get; } = new WeatherPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public WeatherPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            ViewModel.SetUpVM();
        }

        private void Image_Tapped(object sender, TappedRoutedEventArgs e)
        {
            PageService?.NavigateTo(typeof(SettingsPage), Common.GetLocalizedText("LocationSettings/Text"));
        }
    }
}
