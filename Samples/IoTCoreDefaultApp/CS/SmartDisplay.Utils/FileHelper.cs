// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Utils;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Foundation.Diagnostics;
using Windows.Storage;

namespace SmartDisplay.Utils
{
    public static class FileUtil
    {
        public static async Task<bool> SaveFileAsync(string filename, string contents)
        {
            ServiceUtil.LogService?.Write($"Saving file... {filename}");
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.CreateFileAsync(filename, CreationCollisionOption.ReplaceExisting);
                await FileIO.WriteTextAsync(file, contents);
                return true;
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return false;
        }

        public static async Task<string> ReadFileAsync(string filename)
        {
            ServiceUtil.LogService?.Write($"Reading file... {filename}");
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.GetFileAsync(filename);
                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public static async Task<bool> FileExistsAsync(string filename)
        {
            ServiceUtil.LogService?.Write($"Checking if {filename} exists...");
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.TryGetItemAsync(filename);
                return (file != null);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return false;
        }

        public static async Task DeleteFileAsync(string filename)
        {
            ServiceUtil.LogService?.Write($"Deleting file... {filename}");
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var file = await folder.GetFileAsync(filename);
                await file.DeleteAsync();
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }
        }

        public static async Task<string[]> GetFilesAsync()
        {
            ServiceUtil.LogService?.Write($"Getting file list...");
            var list = new List<string>();
            try
            {
                var folder = ApplicationData.Current.LocalFolder;
                var files = await folder.GetFilesAsync();
                foreach (var file in files)
                {
                    list.Add(file.Name);
                }
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return list.ToArray();
        }

        public static async Task<StorageFile> GetFileFromInstalledLocationAsync(string path)
        {
            ServiceUtil.LogService?.Write($"Getting file from: {path}");
            try
            {
                var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                return await folder.GetFileAsync(path);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }

        public static async Task<string> ReadStringFromInstalledLocationAsync(string path)
        {
            try
            {
                var file = await GetFileFromInstalledLocationAsync(path);
                return await FileIO.ReadTextAsync(file);
            }
            catch (Exception ex)
            {
                ServiceUtil.LogService?.Write(ex.ToString(), LoggingLevel.Error);
            }

            return null;
        }
    }
}
