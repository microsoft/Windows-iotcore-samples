// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.ViewModels;

namespace SmartDisplay.Controls
{
    public class SidePopupControlVM : BaseViewModel
    {
        public double Width
        {
            get { return GetStoredProperty<double>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    HorizontalOffset = -value;
                }
            }
        }

        public double Height
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public object Content
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }

        public double HorizontalOffset
        {
            get { return GetStoredProperty<double>(); }
            set { SetStoredProperty(value); }
        }

        public string HeaderText
        {
            get { return GetStoredProperty<string>(); }
            set
            {
                if (SetStoredProperty(value))
                {
                    IsHeaderVisible = !string.IsNullOrEmpty(value);
                }
            }
        }
        public bool IsHeaderVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public SidePopupControlVM() : base()
        {
            HeaderText = string.Empty;
        }
    }
}
