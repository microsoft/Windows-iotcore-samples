// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.ViewModels;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace SmartDisplay.Controls
{
    public class NavBarDataItem : BaseViewModel
    {
        public NavBarDataItem() : base()
        {
        }

        public NavBarDataItem(PageDescriptor descriptor)
        {
            Content = descriptor.Title;
            Icon = descriptor.Tag;
            PageName = descriptor.Type.FullName;
        }

        public string Content
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string Icon
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public string PageName
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        public SolidColorBrush Background
        {
            get { return GetStoredProperty<SolidColorBrush>() ?? new SolidColorBrush(Colors.Transparent); }
            set { SetStoredProperty(value); }
        }
    }
}
