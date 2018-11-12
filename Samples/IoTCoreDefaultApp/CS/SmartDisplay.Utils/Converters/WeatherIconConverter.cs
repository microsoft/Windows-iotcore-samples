// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace SmartDisplay.Utils.Converters
{
    public class WeatherIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            try
            {
                if (value is string valString)
                {
                    if (Uri.IsWellFormedUriString(valString, UriKind.RelativeOrAbsolute) && 
                        Uri.TryCreate(valString, UriKind.RelativeOrAbsolute, out Uri result))
                    {
                        return new Rectangle
                        {
                            Fill = new ImageBrush
                            {
                                ImageSource = new BitmapImage(result),
                                Stretch = Stretch.Uniform,
                            },
                            RadiusX = 10,
                            RadiusY = 10,
                            Width = 50,
                            Height = 50
                        };
                    }
                    else
                    {
                        return new TextBlock
                        {
                            Text = valString
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
