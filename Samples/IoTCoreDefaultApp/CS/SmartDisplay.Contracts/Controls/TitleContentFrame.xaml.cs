// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class TitleContentFrame : UserControl
    {
        public TitleContentFrame()
        {
            InitializeComponent();
            IsScrollViewerEnabled = true;
        }

        public double TitleFontSize { get; set; } = 40;
        
        #region Dependency Properties

        public static readonly DependencyProperty TitleContentProperty =
            DependencyProperty.Register("TitleContent",
                typeof(object),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;
                    contentFrame.TitleTextContent.Content = args.NewValue;
                })
            );

        public object TitleContent
        {
            get { return GetValue(TitleContentProperty); }
            set { SetValue(TitleContentProperty, value); }
        }

        public static readonly DependencyProperty ContentContainerProperty =
            DependencyProperty.Register("ContentContainer",
                typeof(object),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;

                    // Reset the margin and apply it based on whether or not there is a ScrollViewer
                    contentFrame.ContentControl.Margin = new Thickness();
                    if (contentFrame.IsScrollViewerEnabled)
                    {
                        // Wrap the content with a ScrollViewer
                        contentFrame.ContentControl.Content = new ScrollViewer
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Stretch,
                            Content = new ContentControl
                            {
                                HorizontalAlignment = HorizontalAlignment.Stretch,
                                VerticalAlignment = VerticalAlignment.Stretch,
                                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                                Content = args.NewValue,
                                Margin = contentFrame.ContentMargin
                            }
                        };
                    }
                    else
                    {
                        contentFrame.ContentControl.Content = args.NewValue;
                        contentFrame.ContentControl.Margin = contentFrame.ContentMargin;
                    }
                })
            );

        public object ContentContainer
        {
            get { return GetValue(ContentContainerProperty); }
            set { SetValue(ContentContainerProperty, value); }
        }
        
        public new static readonly DependencyProperty VerticalContentAlignmentProperty =
            DependencyProperty.Register("VerticalContentAlignment",
                typeof(VerticalAlignment),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;
                    contentFrame.ContentControl.VerticalContentAlignment = (VerticalAlignment)args.NewValue;
                })
            );

        public new VerticalAlignment VerticalContentAlignment
        {
            get { return (VerticalAlignment)GetValue(VerticalContentAlignmentProperty); }
            set { SetValue(VerticalContentAlignmentProperty, value); }
        }

        public new static readonly DependencyProperty HorizontalContentAlignmentProperty =
            DependencyProperty.Register("HorizontalContentAlignment",
                typeof(HorizontalAlignment),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;
                    contentFrame.ContentControl.HorizontalContentAlignment = (HorizontalAlignment)args.NewValue;
                })
            );

        public new HorizontalAlignment HorizontalContentAlignment
        {
            get { return (HorizontalAlignment)GetValue(HorizontalContentAlignmentProperty); }
            set { SetValue(HorizontalContentAlignmentProperty, value); }
        }

        public static readonly DependencyProperty TitleContentMarginProperty =
            DependencyProperty.Register("TitleContentMargin",
                typeof(Thickness),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;
                    contentFrame.TitleTextContent.Margin = (Thickness)args.NewValue;
                })
            );

        public Thickness TitleContentMargin
        {
            get { return (Thickness)GetValue(TitleContentMarginProperty); }
            set { SetValue(TitleContentMarginProperty, value); }
        }

        public static readonly DependencyProperty ContentMarginProperty =
            DependencyProperty.Register("ContentMargin",
                typeof(Thickness),
                typeof(TitleContentFrame),
                new PropertyMetadata(0, (sender, args) =>
                {
                    var contentFrame = (TitleContentFrame)sender;
                    if (contentFrame.ContentControl.Content is ScrollViewer scrollViewer &&
                        scrollViewer.Content is ContentControl contentControl)
                    {
                        contentControl.Margin = (Thickness)args.NewValue;
                    }
                    else
                    {
                        contentFrame.ContentControl.Margin = (Thickness)args.NewValue;
                    }
                })
            );

        public Thickness ContentMargin
        {
            get { return (Thickness)GetValue(ContentMarginProperty); }
            set { SetValue(ContentMarginProperty, value); }
        }

        public static readonly DependencyProperty IsScrollViewerEnabledProperty =
            DependencyProperty.Register("IsScrollViewerEnabled",
                typeof(bool),
                typeof(TitleContentFrame), null);

        public bool IsScrollViewerEnabled
        {
            get { return (bool)GetValue(IsScrollViewerEnabledProperty); }
            set { SetValue(IsScrollViewerEnabledProperty, value); }
        }

        #endregion
    }
}
