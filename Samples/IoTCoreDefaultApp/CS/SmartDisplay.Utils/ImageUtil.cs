// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace SmartDisplay.Utils
{
    public static class ImageUtil
    {
        public static async Task<IRandomAccessStream> GetBitmapStreamAsync(StorageFile file)
        {
            Debug.WriteLine("GetBitmapStreamAsync for file: " + file.Path);
            return await file.OpenReadAsync().AsTask().ConfigureAwait(false);
        }
    }
}
