// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using System;
using System.Collections.ObjectModel;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views.Settings
{
    public sealed partial class NetworkPropertiesPage : PageBase
    {
        public ObservableCollection<AdapterConfig> AdaptersCollection { get; private set; } = new ObservableCollection<AdapterConfig>();

        public NetworkPropertiesPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                AdaptersCollection = new ObservableCollection<AdapterConfig>(NetworkAdapterUtil.GetAdapters());
            }
            catch (Exception ex)
            {
                AppService?.LogService.WriteException(ex);
            }
        }
    }
}
