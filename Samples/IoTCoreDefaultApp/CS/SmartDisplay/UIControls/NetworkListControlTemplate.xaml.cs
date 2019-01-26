// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class NetworkListControlTemplate : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty SignalBarsProperty =
            DependencyProperty.Register("SignalBars",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.SignalBarsTextBlock.Text = args.NewValue.ToString();
                    }
                })
            );

        public string SignalBars
        {
            get { return (string)GetValue(SignalBarsProperty); }
            set { SetValue(SignalBarsProperty, value); }
        }

        public static readonly DependencyProperty SsidTextProperty =
            DependencyProperty.Register("SsidText",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.SsidTextBlock.Text = args.NewValue.ToString();
                    }
                })
            );

        public string SsidText
        {
            get { return (string)GetValue(SsidTextProperty); }
            set { SetValue(SsidTextProperty, value); }
        }

        public static readonly DependencyProperty PanelContentProperty =
            DependencyProperty.Register("PanelContent",
                typeof(object),
                typeof(NetworkListControlTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is NetworkListControlTemplate template)
                    {
                        template.PanelContentControl.Content = args.NewValue;
                    }
                })
            );

        public object PanelContent
        {
            get { return GetValue(PanelContentProperty); }
            set { SetValue(PanelContentProperty, value); }
        }

        #endregion

        public NetworkListControlTemplate()
        {
            InitializeComponent();
        }
    }
}
