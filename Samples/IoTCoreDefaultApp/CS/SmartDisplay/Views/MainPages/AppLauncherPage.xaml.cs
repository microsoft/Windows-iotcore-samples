// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    public sealed partial class AppLauncherPage : PageBase
    {
        public AppLauncherPageVM ViewModel { get; } = new AppLauncherPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public AppLauncherPage()
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
            if (args.Data is AppListEntry entry)
            {
                AppService.DisplayDialog(
                    Common.GetLocalizedText("AppLauncherTitle/Text"),
                    Common.GetLocalizedText("AppLauncherDialogPrompt"),
                    new DialogButton(Common.GetLocalizedText("YesButton/Content"), async (s, e) => await entry.LaunchAsync())
                );
            }
        }
    }
}
