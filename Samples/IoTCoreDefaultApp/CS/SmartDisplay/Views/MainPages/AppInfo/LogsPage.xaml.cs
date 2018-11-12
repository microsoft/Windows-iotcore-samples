// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Displays the logs that are saved on the device
    /// </summary>
    public sealed partial class LogsPage : PageBase
    {
        public LogsPageVM ViewModel { get; } = new LogsPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public LogsPage()
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

        // We want to bind the ListViewItem.IsSelected property to our data item but UWP doesn't support binding to setters.
        // We can work around that by creating a manual binding when the ListViewItem is created.
        public void LogFileItem_DataContextChanged(object sender, DataContextChangedEventArgs e)
        {
            if (sender is Panel panel)
            {
                ListViewItem item = FindParent<ListViewItem>(panel);
                if (item != null)
                {
                    item.SetBinding(ListViewItem.IsSelectedProperty, new Binding()
                    {
                        Path = new PropertyPath(nameof(LogFile.IsSelected)),
                        Source = panel.DataContext,
                        Mode = BindingMode.TwoWay
                    });
                }
            }
        }
    }
}
