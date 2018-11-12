// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using SmartDisplay.ViewModels;

namespace SmartDisplay.Controls
{
    public class LoadingPanelVM : BaseViewModel
    {
        public bool IsVisible
        {
            get { return GetStoredProperty<bool>(); }
            set { SetStoredProperty(value); }
        }

        public string Text
        {
            get { return GetStoredProperty<string>() ?? Common.GetLocalizedText("LoadingText"); }
            set { SetStoredProperty(value); }
        }
    }
}
