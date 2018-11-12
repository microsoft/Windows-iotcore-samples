// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.ViewModels;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Controls
{
    public class TileButtonVM : BaseViewModel
    {
        public static readonly Color DefaultColor = Color.FromArgb(255, 0, 120, 215);

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
        public bool UseIcon
        {
            get { return GetStoredProperty<bool>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    NotifyPropertyChanged("UseImage");
                }
            }
        }
        public string Icon
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public bool UseImage
        {
            get { return !UseIcon; }
            set { UseIcon = !value; }
        }
        public BitmapImage Image
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }
        public string Title
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }
        public TextWrapping TitleTextWrapping
        {
            get { return GetStoredProperty<TextWrapping>(); }
            set { SetStoredProperty(value); }
        }
        public SolidColorBrush BackgroundColor
        {
            get { return GetStoredProperty<SolidColorBrush>() ?? new SolidColorBrush(DefaultColor); }
            set { SetStoredProperty(value); }
        }
    }
}
