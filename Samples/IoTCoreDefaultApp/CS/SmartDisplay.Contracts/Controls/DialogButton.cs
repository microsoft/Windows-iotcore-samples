// Copyright (c) Microsoft Corporation. All rights reserved.

using Windows.Foundation;
using Windows.UI.Xaml.Controls;

namespace SmartDisplay.Controls
{
    /// <summary>
    /// Wrapper to pass button text and handler to DisplayDialog
    /// </summary>
    public struct DialogButton
    {
        public string Name;
        public TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> ClickEventHandler;

        public DialogButton(string name, TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs> clickEventHandler)
        {
            Name = name;
            ClickEventHandler = clickEventHandler;
        }
    }
}
