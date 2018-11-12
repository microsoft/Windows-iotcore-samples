// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Utils;
using SmartDisplay.ViewModels;
using System;
using Windows.Storage;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Controls
{
    public class JumboNotificationVM : BaseViewModel
    {
        private ILogService LogService => AppService?.LogService;

        public bool GridVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public bool TextVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string Text
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public string Symbol
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
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

        public BitmapImage SymbolImage
        {
            get { return GetStoredProperty<BitmapImage>(); }
            set { SetStoredProperty(value); }
        }

        public bool TrySetSymbolImage(StorageFile file)
        {
            using (var stream = ImageUtil.GetBitmapStreamAsync(file).Result)
            {
                try
                {
                    SymbolImage.SetSource(stream);
                    return true;
                }
                catch (Exception ex)
                {
                    LogService.WriteException(ex);
                }
            }

            return false;
        }

        public bool UseSymbolImage
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public JumboNotificationVM()
        {
            SymbolImage = new BitmapImage();
            Color = JumboNotificationControl.DefaultColor;
        }
    }
}
