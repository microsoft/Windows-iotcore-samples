// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace SmartDisplay.Contracts
{
    public interface ILogService
    {
        /// <summary>
        /// Raises an event when something is written to the LogService
        /// </summary>
        event TypedEventHandler<object, LogOutputEventArgs> LogOutput;

        void Write(
            string text = "", 
            LoggingLevel loggingLevel = LoggingLevel.Verbose, 
            [CallerMemberName] string memberName = "", 
            [CallerFilePath] string sourceFilePath = "", 
            [CallerLineNumber] int sourceLineNumber = 0
            );

        void WriteException(
            Exception exception,
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string sourceFilePath = "",
            [CallerLineNumber] int sourceLineNumber = 0
            );

        Task<StorageFile> SaveLogToFileAsync(string prefix = null);

        Task<StorageFolder> GetLogFolderAsync();
    }

    public class LogOutputEventArgs : EventArgs
    {
        public DateTime Timestamp = DateTime.Now;
        public string Text;
        public LoggingLevel LoggingLevel;

        public string CallerMemberName;
        public string SourceFilePath;
        public int SourceLineNumber;
    }
}
