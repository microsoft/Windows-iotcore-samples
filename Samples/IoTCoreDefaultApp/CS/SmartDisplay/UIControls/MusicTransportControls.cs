// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public class MusicTransportControls : MediaTransportControls
    {
        #region Dependency Properties

        public static readonly DependencyProperty IsShuffleOnProperty =
            DependencyProperty.Register("IsShuffleOn",
                typeof(bool),
                typeof(MusicTransportControls),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MusicTransportControls)sender;
                    if (control.GetTemplateChild(ShuffleButtonName) is AppBarToggleButton button)
                    {
                        button.IsChecked = (bool)args.NewValue;
                    }
                })
            );

        public bool IsShuffleOn
        {
            get { return (bool)GetValue(IsShuffleOnProperty); }
            set { SetValue(IsShuffleOnProperty, value); }
        }

        public static readonly DependencyProperty IsRepeatOnProperty =
            DependencyProperty.Register("IsRepeatOn",
                typeof(bool),
                typeof(MusicTransportControls),
                new PropertyMetadata(null, (sender, args) =>
                {
                    var control = (MusicTransportControls)sender;
                    if (control.GetTemplateChild(RepeatButtonName) is AppBarToggleButton button)
                    {
                        button.IsChecked = (bool)args.NewValue;
                    }
                })
            );

        public bool IsRepeatOn
        {
            get { return (bool)GetValue(IsRepeatOnProperty); }
            set { SetValue(IsRepeatOnProperty, value); }
        }

        #endregion

        private const string RepeatButtonName = "CustomRepeatButton";
        private const string ShuffleButtonName = "ShuffleButton";

        public MusicTransportControls()
        {
            DefaultStyleKey = typeof(MusicTransportControls);
        }

        protected override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            if (GetTemplateChild(ShuffleButtonName) is AppBarToggleButton shuffleButton)
            {
                shuffleButton.IsChecked = IsShuffleOn;
                shuffleButton.Checked += ShuffleButton_Checked;
                shuffleButton.Unchecked += ShuffleButton_Checked;
            }

            if (GetTemplateChild(RepeatButtonName) is AppBarToggleButton repeatButton)
            {
                repeatButton.IsChecked = IsRepeatOn;
                repeatButton.Checked += RepeatButton_Checked;
                repeatButton.Unchecked += RepeatButton_Checked;
            }
        }

        private void RepeatButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is AppBarToggleButton button)
            {
                IsRepeatOn = button.IsChecked == true;
            }
        }

        private void ShuffleButton_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is AppBarToggleButton button)
            {
                IsShuffleOn = button.IsChecked == true;
            }
        }
    }
}
