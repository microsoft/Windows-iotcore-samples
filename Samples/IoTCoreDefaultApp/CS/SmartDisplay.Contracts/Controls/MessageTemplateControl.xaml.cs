// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class MessageTemplateControl : UserControl
    {
        #region Dependency Properties

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title",
                typeof(string),
                typeof(MessageTemplateControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MessageTemplateControl)sender;
                    control.TitleTextBlock.Text = (args.NewValue as string) ?? string.Empty;
                })
            );

        public object Title
        {
            get { return GetValue(TitleProperty); }
            set { SetValue(TitleProperty, value); }
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register("Subtitle",
                typeof(string),
                typeof(MessageTemplateControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MessageTemplateControl)sender;
                    control.SubtitleTextBlock.Text = (args.NewValue as string) ?? string.Empty;
                })
            );

        public object Subtitle
        {
            get { return GetValue(SubtitleProperty); }
            set { SetValue(SubtitleProperty, value); }
        }

        public static readonly DependencyProperty MainContentProperty =
            DependencyProperty.Register("MainContent",
                typeof(string),
                typeof(MessageTemplateControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MessageTemplateControl)sender;
                    control.MainContentControl.Content = args.NewValue;
                })
            );

        public object MainContent
        {
            get { return GetValue(MainContentProperty); }
            set { SetValue(MainContentProperty, value); }
        }

        public static readonly DependencyProperty AddContentProperty =
            DependencyProperty.Register("AdditionalContent",
                typeof(string),
                typeof(MessageTemplateControl),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MessageTemplateControl)sender;
                    control.AdditionalContentControl.Content = args.NewValue;
                })
            );

        public object AdditionalContent
        {
            get { return GetValue(AddContentProperty); }
            set { SetValue(AddContentProperty, value); }
        }

        #endregion

        public MessageTemplateControl()
        {
            InitializeComponent();
        }
    }
}
