// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using Windows.Foundation;

namespace SmartDisplay.Contracts
{
    public interface ITelemetryService
    {
        event TypedEventHandler<object, TelemetryEventSentArgs> EventSent;

        bool IsEnabled { get; set; }

        void WriteEvent(string eventName);

        void WriteEvent<T>(string eventName, T data);

        void WriteException(
            Exception exception,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            );
    }

    public class TelemetryEventSentArgs : EventArgs
    {
        public string EventName;
        public object Data;
    }
}
