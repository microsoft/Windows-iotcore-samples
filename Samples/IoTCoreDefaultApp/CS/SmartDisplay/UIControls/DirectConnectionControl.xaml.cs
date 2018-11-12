// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class DirectConnectionControl : UserControl
    {
        private int? _subtitleFontSize = null;
        public int SubtitleFontSize
        {
            get
            {
                if (_subtitleFontSize == null)
                {
                    _subtitleFontSize = 20;
                }

                return (int)_subtitleFontSize;
            }

            set
            {
                _subtitleFontSize = value;
            }
        }

        public DirectConnectionControl()
        {
            InitializeComponent();
        }

        public void SetUpDirectConnection()
        {
            var ethernetProfile = NetworkPresenter.GetDirectConnectionName();

            if (ethernetProfile == null)
            {
                NoneFoundText.Visibility = Visibility.Visible;
                DirectConnectionStackPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                NoneFoundText.Visibility = Visibility.Collapsed;
                DirectConnectionStackPanel.Visibility = Visibility.Visible;
            }
        }
    }
}
