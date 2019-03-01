// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.Presenters;
using SmartDisplay.Utils;
using SmartDisplay.Views.DevicePortal;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace SmartDisplay.ViewModels
{
    public class BlinkyPageVM : BaseViewModel
    {
        #region UI properties
        public ObservableCollection<InfoDisplayData> InfoCollection { get; } = new ObservableCollection<InfoDisplayData>();
        #endregion

        public BlinkyPageVM() : base()
        {
        }

    }
}
