// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Page that displays photos located in the Pictures Library
    /// </summary>
    public sealed partial class PhotoPage : PageBase
    {
        public PhotoPageVM ViewModel { get; } = new PhotoPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public PhotoPage()
        {
            InitializeComponent();
        }

        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await ViewModel.SetUpVM();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);
            ViewModel.TearDownVM();
        }

        private void ImageGrid_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (sender is Grid grid)
            {
                var width = grid.ActualWidth;
                var position = e.GetCurrentPoint(grid).Position;

                if (position.X < (width / 2))
                {
                    // Display previous image
                    ViewModel.UpdateSlideshowImage(true);
                }
                else
                {
                    // Display next image
                    ViewModel.UpdateSlideshowImage(false);
                }

                e.Handled = true;
            }
        }
    }
}
