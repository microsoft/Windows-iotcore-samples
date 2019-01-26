// Copyright (c) Microsoft Corporation. All rights reserved.

using System;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;

namespace SmartDisplay.Features.WinML
{
    public static class MnistHelper
    {
        public static async Task<VideoFrame> GetHandWrittenImageAsync(Grid grid)
        {
            RenderTargetBitmap renderBitmap = new RenderTargetBitmap();

            await renderBitmap.RenderAsync(grid);
            var buffer = await renderBitmap.GetPixelsAsync();
            using (var softwareBitmap = SoftwareBitmap.CreateCopyFromBuffer(
                    buffer, 
                    BitmapPixelFormat.Bgra8,
                    renderBitmap.PixelWidth, 
                    renderBitmap.PixelHeight, 
                    BitmapAlphaMode.Ignore))
            {
                buffer = null;
                renderBitmap = null;

                using (VideoFrame vf = VideoFrame.CreateWithSoftwareBitmap(softwareBitmap))
                {
                    // The MNIST model takes a 28 x 28 image as input
                    return await CropAndDisplayInputImageAsync(vf, new Size(28, 28));
                }
            }
        }

        private static async Task<VideoFrame> CropAndDisplayInputImageAsync(VideoFrame inputVideoFrame, Size targetSize)
        {
            bool useDX = inputVideoFrame.SoftwareBitmap == null;

            var frameHeight = useDX ? inputVideoFrame.Direct3DSurface.Description.Height : inputVideoFrame.SoftwareBitmap.PixelHeight;
            var frameWidth = useDX ? inputVideoFrame.Direct3DSurface.Description.Width : inputVideoFrame.SoftwareBitmap.PixelWidth;

            var requiredAR = targetSize.Width / targetSize.Height;
            uint w = Math.Min((uint)(requiredAR * frameHeight), (uint)frameWidth);
            uint h = Math.Min((uint)(frameWidth / requiredAR), (uint)frameHeight);

            var cropBounds = new BitmapBounds
            {
                X = (uint)((frameWidth - w) / 2),
                Y = 0,
                Width = w,
                Height = h
            };

            var croppedVf = new VideoFrame(BitmapPixelFormat.Bgra8, (int)targetSize.Width, (int)targetSize.Height, BitmapAlphaMode.Ignore);

            await inputVideoFrame.CopyToAsync(croppedVf, cropBounds, null);

            return croppedVf;
        }
    }
}
