// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI.Xaml.Data;

namespace SmartDisplay.Utils.Converters
{
    public class StringToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is string valueString) ||
                !(parameter is string parameterString))
            {
                return false;
            }

            return string.Equals(valueString, parameterString);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return parameter;
        }
    }
}
