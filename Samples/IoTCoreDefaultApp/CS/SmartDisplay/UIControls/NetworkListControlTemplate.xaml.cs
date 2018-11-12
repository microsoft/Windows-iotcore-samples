// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class NetworkListControlTemplate : UserControl
    {
        private NetworkListControlTemplateVM ViewModel { get; } = new NetworkListControlTemplateVM();

        #region Dependency Properties

        public static readonly DependencyProperty SignalBarsProperty =
            DependencyProperty.Register("SignalBars",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.SignalBars = args.NewValue.ToString();
                    }
                })
            );

        public static readonly DependencyProperty SsidTextProperty =
            DependencyProperty.Register("SsidText",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.SsidText = args.NewValue.ToString();
                    }
                })
            );

        public static readonly DependencyProperty PanelContentProperty =
            DependencyProperty.Register("PanelContent",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.PanelContent = args.NewValue;
                    }
                })
            );

        #endregion

        public string SignalBars
        {
            get { return ViewModel.SignalBars; }
            set { ViewModel.SignalBars = value; }
        }

        public string SsidText
        {
            get { return ViewModel.SsidText; }
            set { ViewModel.SsidText = value; }
        }

        public object PanelContent
        {
            get { return ViewModel.PanelContent; }
            set { ViewModel.PanelContent = value; }
        }

        public NetworkListControlTemplate()
        {
            InitializeComponent();
        }
    }
}
