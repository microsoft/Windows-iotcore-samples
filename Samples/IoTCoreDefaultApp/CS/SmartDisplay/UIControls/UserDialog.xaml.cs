// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls.ViewModels;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    public sealed partial class UserDialog : ContentDialog
    {
        private UserDialogVM ViewModel { get; } = new UserDialogVM();

        /// <summary>
        /// Set to true if the primary button was selected, otherwise false.
        /// </summary>
        public bool Result => ViewModel.Result;

        public UserDialog(string title, string question, string primaryButtonText, string secondaryButtonText)
        {
            InitializeComponent();

            Title = title;
            PrimaryButtonText = primaryButtonText;
            SecondaryButtonText = secondaryButtonText;

            ViewModel.SetUpVM(question);
        }
    }
}
