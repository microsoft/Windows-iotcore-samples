// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI.Xaml.Data;

namespace SmartDisplay.Utils.Converters
{
    public class WiFiGlyphConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is byte strength)
            {
                switch (strength)
                {
                    case 0:
                        return "\xE904";
                    case 1:
                        return "\xE905";
                    case 2:
                    case 3:
                        return "\xE906";
                    case 4:
                        return "\xE907";
                    default:
                        return "\xE908";
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return null;
        }
    }
}
