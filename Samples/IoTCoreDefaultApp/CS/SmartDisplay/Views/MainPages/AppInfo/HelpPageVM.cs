// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;

namespace SmartDisplay.ViewModels
{
    public class HelpPageVM : BaseViewModel
    {
        #region UI properties

        public string HelpText
        {
            get { return GetStoredProperty<string>() ?? string.Empty; }
            set { SetStoredProperty(value); }
        }

        #endregion

        public HelpPageVM() : base()
        {
            HelpText = Common.GetLocalizedText("HelpPagePlaceholderText");
        }

        public void SetUpVM(string msg)
        {
            HelpText = msg;
        }
    }
}
