// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.ViewModels;
using System.Windows.Input;

namespace SmartDisplay.Controls.ViewModels
{
    public class UserDialogVM : BaseViewModel
    {
        /// <summary>
        /// Set to true if the primary button was selected, otherwise false.
        /// </summary>
        public bool Result { get; private set; }

        public UserDialogVM()
        {
        }

        public void SetUpVM(string question)
        {
            Question = question;
        }

        #region UI properties and commands

        public string Question
        {
            get { return GetStoredProperty<string>(); }
            set { SetStoredProperty(value); }
        }

        private RelayCommand<bool> _setResultCommand;
        public ICommand SetResultCommand
        {
            get
            {
                return _setResultCommand ??
                    (_setResultCommand = new RelayCommand<bool>(result =>
                    {
                        Result = result;
                    }));
            }
        }

        #endregion
    }
}
