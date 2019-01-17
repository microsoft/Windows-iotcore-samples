// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI;
using Windows.UI.Xaml.Data;

namespace SmartDisplay.Utils.Converters
{
    public class NumberToTemperatureStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return $"{value}°";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class DateToDayOfWeekConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
            {
                return dateTime.ToString("ddd").ToUpper();
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }

    public class WeatherIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string emoji)
            {
                switch (emoji)
                {
                    case "☀️":
                    case "⛅":
                        return Color.FromArgb(255, 244, 251, 63);
                    case "🌧️":
                        // Rain
                        return Color.FromArgb(255, 67, 223, 255);
                    case "☁️":
                    case "🌩️":
                    case "🌫️":
                    case "🌨️":
                    case "🍃":
                    default:
                        return Colors.White;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }


    public class EmojiToWeatherIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string emoji)
            {
                switch (emoji)
                {
                    case "☁️":
                        return "\uE9BE";
                    case "☀️":
                        return "\uE9BD";
                    case "🌫️":
                        return "\uE9CB";
                    case "🌧️":
                        // Rain
                        return "\uE9C4";
                    case "🌩️":
                        return "\uE9C6";
                    case "🌨️":
                        return "\uE9C8";
                    case "🍃":
                        return "\uE9CC";
                    case "⛅":
                        return "\uE9C0";
                    default:
                        return emoji;
                }
            }

            return string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
