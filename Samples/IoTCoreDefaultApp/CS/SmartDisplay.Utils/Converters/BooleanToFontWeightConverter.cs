// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using Windows.UI.Text;
using Windows.UI.Xaml.Data;

namespace SmartDisplay.Utils.Converters
{
    public class BooleanToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter != null && parameter.ToString().Equals("Invert");
            return (value is bool && (invert ^ (bool)(value))) ? FontWeights.Bold : FontWeights.Light;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter != null && parameter.ToString().Equals("Invert");
            bool isBold = (value is FontWeight fontWeight) && (fontWeight.Weight == FontWeights.Bold.Weight);
            return invert ^ isBold;
        }
    }
}
