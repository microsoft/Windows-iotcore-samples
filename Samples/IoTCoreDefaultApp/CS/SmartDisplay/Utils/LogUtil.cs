// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Identity;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.Storage;
using Windows.Storage.Search;

namespace SmartDisplay.Utils
{
    public static class LogUtil
    {
        public static async Task<string> EmailLogsAsync(GraphHelper graphHelper, string subject, string messageContent, StorageFile[] files)
        {
            if (files == null || files.Length < 1)
            {
                // The UI should prevent this from happening.
                throw new InvalidOperationException("No files have been provided");
            }

            var user = await graphHelper.GetUserAsync();
            await graphHelper.SendEmailAsync(user.Mail, subject, messageContent, files);

            return user.Mail;
        }

        public static async Task<StorageFileQueryResult> GetLogFilesQueryResultAsync(uint maxNumberOfFiles = 1000)
        {
            // Initialize queryOptions using a common query
            QueryOptions queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new string[] { ".etl" });

            // Clear all existing sorts
            queryOptions.SortOrder.Clear();

            // Add descending sort by date modified
            SortEntry se = new SortEntry
            {
                PropertyName = "System.DateModified",
                AscendingOrder = false,
            };
            queryOptions.SortOrder.Add(se);

            var logFolder = await App.LogService.GetLogFolderAsync();
            return logFolder.CreateFileQueryWithOptions(queryOptions);
        }

        public static async Task<StorageFile[]> GetLogFilesAsync(uint maxNumberOfFiles = 1000)
        {
            var queryResult = await GetLogFilesQueryResultAsync();

            // Get only the N most recent logs
            var logFiles = await queryResult.GetFilesAsync(0, maxNumberOfFiles);

            return logFiles.ToArray();
        }

        public static string CreateMessageContent(string currentPage, string contentTemplate = null)
        {
            var deviceInfo = new EasClientDeviceInformation();

            string nl = Environment.NewLine;
            string sysInfo =
                $"Current Page: {currentPage}" + nl +
                $"Package Name: {Package.Current.DisplayName + nl}" +
                $"Package Version: {Common.GetAppVersion() + nl}" +
                $"OS: {deviceInfo.OperatingSystem + nl}" +
                $"OS Version: {Common.GetOSVersionString() + nl}" +
                $"Manufacturer: {deviceInfo.SystemManufacturer + nl}" +
                $"Model: {deviceInfo.SystemProductName + nl}" +
                $"SKU: {deviceInfo.SystemSku + nl}" +
                $"Firmware Version: {deviceInfo.SystemFirmwareVersion + nl}" +
                $"Hardware Version: {deviceInfo.SystemHardwareVersion + nl}";

            return (!string.IsNullOrEmpty(contentTemplate)) ? contentTemplate.Replace("[[SYSTEM_INFORMATION]]", sysInfo) : sysInfo;
        }
    }
}
