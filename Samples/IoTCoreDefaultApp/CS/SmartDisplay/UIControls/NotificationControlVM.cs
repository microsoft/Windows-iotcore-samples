// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.ViewModels;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    public class NotificationControlVM : BaseViewModel
    {
        public bool IsVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string Text
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string Icon
        {
            get { return GetStoredProperty<string>() ?? "⚪"; }
            set { SetStoredProperty(value); }
        }

        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public double Height
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public Color Color
        {
            get { return GetStoredProperty<Color>(); }
            set { SetStoredProperty(value); }
        }

        public NotificationControlVM()
        {
            Color = JumboNotificationControl.DefaultColor;
        }
    }
}
