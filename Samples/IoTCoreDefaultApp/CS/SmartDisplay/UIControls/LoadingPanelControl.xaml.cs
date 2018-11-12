// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class LoadingPanelControl : UserControl
    {
        private LoadingPanelVM ViewModel { get; } = new LoadingPanelVM();
        
        public LoadingPanelControl()
        {
            InitializeComponent();

            ViewModel.Text = Common.GetLocalizedText("LoadingText");
            ViewModel.IsVisible = false;
        }

        public void Show(string text)
        {
            ViewModel.Text = text;
            ViewModel.IsVisible = true;
        }

        public void Hide()
        {
            ViewModel.IsVisible = false;
        }
    }
}
