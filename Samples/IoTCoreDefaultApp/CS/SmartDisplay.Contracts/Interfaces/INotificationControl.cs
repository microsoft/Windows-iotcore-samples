// Copyright (c) Microsoft Corporation. All rights reserved.

using System;

namespace SmartDisplay.Contracts.Interfaces
{
    public interface INotificationControl
    {
        event EventHandler NotificationPressed;

        void Show(string text, int timeoutMs, string icon);

        void Hide();
    }
}
