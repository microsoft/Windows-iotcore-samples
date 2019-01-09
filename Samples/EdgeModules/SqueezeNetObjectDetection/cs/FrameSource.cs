using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Media.MediaProperties;

using Helpers;
using static Helpers.AsyncHelper;

//
// This sample directly implements the following:
//
// https://docs.microsoft.com/en-us/windows/uwp/audio-video-camera/process-media-frames-with-mediaframereader
//

namespace SampleModule
{
    class FrameSource: IDisposable
    {
        private MediaCapture mediaCapture = null;
        private MediaFrameReader mediaFrameReader = null;
        private EventWaitHandle evtFrame = null;
        public static async Task<IEnumerable<string>> GetSourceNamesAsync()
        {
            var frameSourceGroups = await AsAsync(MediaFrameSourceGroup.FindAllAsync());
            return frameSourceGroups.Select(x=>x.DisplayName);
        }

        public async Task StartAsync(string Name)
        {
            var frameSourceGroups = await AsAsync(MediaFrameSourceGroup.FindAllAsync());
            var selectedGroup = frameSourceGroups.Where(x=>x.DisplayName.Contains(Name)).FirstOrDefault();

            if (null == selectedGroup)
                throw new ApplicationException($"Unable to find frame source named {Name}");

            var colorSourceInfo = selectedGroup.SourceInfos
                .Where(x=>x.MediaStreamType == MediaStreamType.VideoRecord && x.SourceKind == MediaFrameSourceKind.Color)
                .FirstOrDefault();

            if (null == colorSourceInfo)
                throw new ApplicationException($"Unable to find color video recording source on {Name}");

            mediaCapture = new MediaCapture();

            if (null == mediaCapture)
                throw new ApplicationException($"Unable to create new mediacapture");

            var settings = new MediaCaptureInitializationSettings()
            {
                SourceGroup = selectedGroup,
                SharingMode = MediaCaptureSharingMode.ExclusiveControl,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu,
                StreamingCaptureMode = StreamingCaptureMode.Video
            };
            try
            {
                await AsAsync(mediaCapture.InitializeAsync(settings));
            }
            catch (Exception ex)
            {
                throw new ApplicationException("MediaCapture initialization failed: " + ex.Message, ex);
            }

            var colorFrameSource = mediaCapture.FrameSources[colorSourceInfo.Id];

            var preferredFormat = colorFrameSource.SupportedFormats.Where(format => format.VideoFormat.Width >= 1080 && format.Subtype == "NV12").FirstOrDefault();

            if (null == preferredFormat)
                throw new ApplicationException("Our desired format is not supported");

            await AsAsync(colorFrameSource.SetFormatAsync(preferredFormat));

            mediaFrameReader = await AsAsync(mediaCapture.CreateFrameReaderAsync(colorFrameSource, MediaEncodingSubtypes.Argb32));

            if (null == mediaFrameReader)
                throw new ApplicationException($"Unable to create new mediaframereader");

            evtFrame = new EventWaitHandle(false, EventResetMode.ManualReset);
            mediaFrameReader.FrameArrived += (s,a) => evtFrame.Set();
            await AsAsync(mediaFrameReader.StartAsync());

            Log.WriteLineVerbose("FrameReader Started");
        }

        public async Task<MediaFrameReference> GetFrameAsync()
        {
            MediaFrameReference result = null;
            do
            {
                evtFrame.WaitOne();
                evtFrame.Reset();

                result = mediaFrameReader.TryAcquireLatestFrame();

                if (null == result)
                    await Task.Delay(10);
            }
            while (null == result);

            return result;            
        }

        public async Task StopAsync()
        {
            await AsAsync(mediaFrameReader.StopAsync());

            Log.WriteLineVerbose("FrameReader Stopped");
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // dispose managed state (managed objects).
                    mediaFrameReader?.Dispose();
                    mediaFrameReader = null;

                    mediaCapture?.Dispose();
                    mediaCapture = null;

                    evtFrame?.Dispose();
                    evtFrame = null;
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~FrameSource() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
