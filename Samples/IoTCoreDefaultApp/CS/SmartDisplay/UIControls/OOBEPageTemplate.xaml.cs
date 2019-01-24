// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Windows.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class OOBEPageTemplate : UserControl
    {
        private OOBEPageTemplateVM ViewModel { get; } = new OOBEPageTemplateVM();

        #region Dependency Properties

        public static readonly DependencyProperty TitleTextProperty =
            DependencyProperty.Register("TitleText",
                typeof(object),
                typeof(OOBEPageTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is OOBEPageTemplate template)
                    {
                        template.TitleText = args.NewValue.ToString();
                    }
                })
            );

        public static readonly DependencyProperty SubtitleTextProperty =
            DependencyProperty.Register("SubtitleText",
                typeof(object),
                typeof(OOBEPageTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is OOBEPageTemplate template)
                    {
                        template.SubtitleText = args.NewValue.ToString();
                    }
                })
            );

        public static readonly DependencyProperty PanelContentProperty =
            DependencyProperty.Register("PanelContent",
                typeof(object),
                typeof(OOBEPageTemplate),
                new PropertyMetadata(0, (sender, args) =>
                {
                    if (sender is OOBEPageTemplate template)
                    {
                        template.PanelContent = args.NewValue;
                    }
                })
            );

        #endregion

        public string TitleText
        {
            get { return ViewModel.TitleText; }
            set { ViewModel.TitleText = value; }
        }

        public string SubtitleText
        {
            get { return ViewModel.SubtitleText; }
            set { ViewModel.SubtitleText = value; }
        }

        public string NextButtonText
        {
            get { return ViewModel.NextButtonText; }
            set { ViewModel.NextButtonText = value; }
        }

        public string TimeoutText
        {
            get { return ViewModel.TimeoutText; }
            set { ViewModel.NextButtonText = value; }
        }

        public object PanelContent
        {
            get { return ViewModel.PanelContent; }
            set { ViewModel.PanelContent = value; }
        }

        public ICommand NextButtonCommand
        {
            get { return ViewModel.NextButtonCommand; }
            set { ViewModel.NextButtonCommand = value; }
        }

        public EventHandler NextButtonClick;

        public OOBEPageTemplate()
        {
            InitializeComponent();
            Loaded += OOBEPageTemplate_Loaded;
            Unloaded += OOBEPageTemplate_Unloaded;
        }

        private void OOBEPageTemplate_Loaded(object sender, RoutedEventArgs e)
        {
            ViewModel.StartTimeoutTimer();
        }

        private void OOBEPageTemplate_Unloaded(object sender, RoutedEventArgs e)
        {
            ViewModel.CancelCountdown();
        }
    }
}
