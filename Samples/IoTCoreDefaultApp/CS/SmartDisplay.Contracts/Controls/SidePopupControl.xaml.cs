// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class SidePopupControl : UserControl
    {
        private SidePopupControlVM ViewModel { get; } = new SidePopupControlVM();
        
        // Dependency property is required for TwoWay binding
        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register("IsOpen",
                typeof(bool),
                typeof(SidePopupControl),
                new PropertyMetadata(false, (sender, args) =>
                {
                    var control = (SidePopupControl)sender;
                    control.PopupControl.IsOpen = (bool)args.NewValue;
                })
            );

        public bool IsOpen
        {
            get { return (bool)GetValue(IsOpenProperty); }
            set { SetValue(IsOpenProperty, value); }
        }

        public new double Width
        {
            get { return ViewModel.Width; }
            set { ViewModel.Width = value; }
        }

        public new double Height
        {
            get { return ViewModel.Height; }
            set { ViewModel.Height = value; }
        }

        public new object Content
        {
            get { return ViewModel.Content; }
            set { ViewModel.Content = value; }
        }

        public string HeaderText
        {
            get { return ViewModel.HeaderText; }
            set { ViewModel.HeaderText = value; }
        }

        public SidePopupControl()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            IsOpen = false;
        }
    }
}
