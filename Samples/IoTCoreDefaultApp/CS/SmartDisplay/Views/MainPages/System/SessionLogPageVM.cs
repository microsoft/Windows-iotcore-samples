// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using SmartDisplay.Logging;
using System.Collections.ObjectModel;
using System.Linq;

namespace SmartDisplay.ViewModels
{
    public class SessionLogPageVM : BaseViewModel
    {
        #region UI properties and commands

        public ObservableCollection<LogOutputEventArgs> LogEntries => new ObservableCollection<LogOutputEventArgs>(((EtwLogService)App.LogService).LogEntries.ToArray().Reverse());

        #endregion
    }
}
