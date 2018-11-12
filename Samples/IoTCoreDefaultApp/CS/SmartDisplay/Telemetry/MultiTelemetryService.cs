// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Windows.Foundation;
using Windows.System.Threading;

namespace SmartDisplay.Telemetry
{
    /// <summary>
    /// Serves as a container for other ITelemetryService objects
    /// if you choose to use multiple telemetry providers
    /// </summary>
    public class MultiTelemetryService : ITelemetryService
    {
        public event TypedEventHandler<object, TelemetryEventSentArgs> EventSent;

        public bool IsEnabled { get; set; } = true;

        private List<ITelemetryService> _services = new List<ITelemetryService>();

        public MultiTelemetryService(params ITelemetryService[] services)
        {
            _services.AddRange(services);
            _services.AddRange(AppComposer.Imports.TelemetryServices);
        }

        public void WriteEvent(string eventName)
        {
            if (!IsEnabled)
            {
                return;
            }

            // Write the telemetry on a separate low priority thread
            var unused = ThreadPool.RunAsync((s) =>
            {
                foreach (var service in _services)
                {
                    service.WriteEvent(eventName);
                }

                EventSent?.Invoke(this, new TelemetryEventSentArgs()
                {
                    EventName = eventName
                });
            }, WorkItemPriority.Low);
        }

        public void WriteEvent<T>(string eventName, T data)
        {
            if (!IsEnabled)
            {
                return;
            }

            // Write the telemetry on a separate low priority thread
            var unused = ThreadPool.RunAsync((s) =>
            {
                foreach (var service in _services)
                {
                    service.WriteEvent(eventName, data);
                }

                EventSent?.Invoke(this, new TelemetryEventSentArgs()
                {
                    EventName = eventName,
                    Data = data
                });
            }, WorkItemPriority.Low);
        }

        public void WriteException(
            Exception exception,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            if (!IsEnabled)
            {
                return;
            }

            // Write the telemetry on a separate low priority thread
            var unused = ThreadPool.RunAsync((s) =>
            {
                foreach (var service in _services)
                {
                    service.WriteException(exception, memberName, sourceFilePath, sourceLineNumber);
                }

                EventSent?.Invoke(this, new TelemetryEventSentArgs()
                {
                    EventName = exception.GetType().Name,
                    Data = exception
                });
            }, WorkItemPriority.Low);
        }
    }
}
