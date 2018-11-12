// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class TilePage : PageBase
    {
        public TilePageVM ViewModel { get; } = new TilePageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public TilePage()
        {
            InitializeComponent();
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

        private void TileGridView_ItemClick(object sender, TileGridItem args)
        {
            if (args.Data is PageDescriptor descriptor)
            {
                PageService?.NavigateTo(descriptor.Type);
            }
        }
    }
}
