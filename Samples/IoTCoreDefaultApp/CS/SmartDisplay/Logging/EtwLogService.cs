// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace SmartDisplay.Logging
{
    public class EtwLogService : ILogService
    {
        /// <summary>
        /// Raises an event when something is written to the LogService
        /// </summary>
        public event TypedEventHandler<object, LogOutputEventArgs> LogOutput;

        public Queue<LogOutputEventArgs> LogEntries { get; private set; } = new Queue<LogOutputEventArgs>();
        
        private ITelemetryService _telemetryService = null;
        private object _entryLock = new object();

        // Maximum number of entries to keep in the log history
        private const int MaxEntries = 50;

        private const string LogFolderName = "Logs";

        private LoggingChannel _channel;
        private LoggingSession _session;

        public EtwLogService(string sessionName, Guid guid)
        {
            _session = new LoggingSession(sessionName);
            _channel = new LoggingChannel(sessionName + "_Channel", null, guid);
            _session.AddLoggingChannel(_channel);
        }

        public void Write(
            string text = "",
            LoggingLevel loggingLevel = LoggingLevel.Verbose, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string sourceFilePath = "", 
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            WriteLog(
                text,
                loggingLevel,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );

            // Send telemetry for Critical or Error logging events
            switch (loggingLevel)
            {
                case LoggingLevel.Critical:
                case LoggingLevel.Error:
                    _telemetryService?.WriteEvent(
                        Enum.GetName(typeof(LoggingLevel), loggingLevel) + "Event",
                        new
                        {
                            Text = text,
                            MemberName = memberName,
                            SourceFilePath = sourceFilePath,
                            SourceLineNumber = sourceLineNumber
                        });
                    break;
            }
                        
            InsertEntry(new LogOutputEventArgs()
            {
                Text = text,
                LoggingLevel = loggingLevel,
                CallerMemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber
            });
        }

        public void WriteException(
            Exception exception,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            )
        {
            var text = exception.ToString();
            var loggingLevel = LoggingLevel.Error;

            WriteLog(
                text,
                loggingLevel,
                memberName,
                sourceFilePath,
                sourceLineNumber
                );

            _telemetryService?.WriteException(exception);
            
            InsertEntry(new LogOutputEventArgs()
            {
                Text = text,
                LoggingLevel = loggingLevel,
                CallerMemberName = memberName,
                SourceFilePath = sourceFilePath,
                SourceLineNumber = sourceLineNumber
            });
        }

        public async Task<StorageFile> SaveLogToFileAsync(string prefix = null)
        {
            if (_session == null || _channel == null)
            {
                Debug.WriteLine("Error: Logger is not initialized");
                return null;
            }

            var fileName = prefix + "_" + DateTime.UtcNow.ToString("yyyy-MM-ddTHHmmss") + ".etl";

            var logFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(LogFolderName, CreationCollisionOption.OpenIfExists);
            
            return await _session.SaveToFileAsync(logFolder, fileName);
        }

        public async Task<StorageFolder> GetLogFolderAsync()
        {
            return await ApplicationData.Current.LocalFolder.CreateFolderAsync(LogFolderName, CreationCollisionOption.OpenIfExists);
        }

        public void SetTelemetryService(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        private void WriteLog(string text,
            LoggingLevel loggingLevel = LoggingLevel.Verbose,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0)
        {
            Debug.WriteLine($"[Log::{Enum.GetName(typeof(LoggingLevel), loggingLevel)}][{memberName}] {text}");

            if (_session == null || _channel == null)
            {
                Debug.WriteLine("Error: Logger is not initialized");
                return;
            }

            try
            {
                string message =
                    "Message: " + text + Environment.NewLine + Environment.NewLine +
                    "Member name: " + memberName + Environment.NewLine +
                    "Source file path: " + sourceFilePath + Environment.NewLine +
                    "Source line number: " + sourceLineNumber + Environment.NewLine;

                var fields = new LoggingFields();
                fields.AddString("MemberName", memberName);
                fields.AddString("SourceFilePath", sourceFilePath);
                fields.AddInt32("SourceLineNumber", sourceLineNumber);
                fields.AddString("Message", text);

                _channel.LogEvent("LogEvent", fields, loggingLevel);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[Log] " + ex);
            }
        }

        private void InsertEntry(LogOutputEventArgs logEntry)
        {
            LogOutput?.Invoke(this, logEntry);

            lock (_entryLock)
            {
                // Remove entries if there are too many
                while (LogEntries.Count > MaxEntries)
                {
                    LogEntries.Dequeue();
                }
                
                LogEntries.Enqueue(logEntry);
            }
        }
    }
}
