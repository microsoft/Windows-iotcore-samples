// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public class CommandBarButton
    {
        public static CommandBarButton Separator = new CommandBarButton();

        public IconElement Icon { get; set; }
        public string Label { get; set; }
        public RoutedEventHandler Handler { get; set; }
    }
}
