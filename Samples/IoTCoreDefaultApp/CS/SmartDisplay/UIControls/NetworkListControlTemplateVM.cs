// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.ViewModels;

namespace SmartDisplay.Controls
{
    public class NetworkListControlTemplateVM : BaseViewModel
    {
        public string SignalBars
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }        

        public string SsidText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        public object PanelContent
        {
            get { return GetStoredProperty<object>(); }
            set { SetStoredProperty(value); }
        }
    }
}
