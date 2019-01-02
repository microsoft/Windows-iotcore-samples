using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using static Helpers.AsyncHelper;
using Log = System.Console;

namespace SampleModule
{
    public class Camera: IDisposable
    {
        public static async Task<IReadOnlyList<MediaFrameSourceGroup>> EnumFrameSourcesAsync() => await AsAsync(MediaFrameSourceGroup.FindAllAsync());

        public static void ListFrameSources(IReadOnlyList<MediaFrameSourceGroup> sources)
        {
            Log.WriteLine("Found {0} Cameras", sources.Count);
            foreach (var g in sources)
            {
                var sourceinfos = g.SourceInfos;
                Log.WriteLine($"{g.DisplayName}");
                /*
                Log.WriteLine("             with {0} Sources:", sourceinfos.Count);

                foreach (var s in sourceinfos)
                {
                    var d = s.DeviceInformation;
                    Log.WriteLine("\t{0}", s.Id);
                    Log.WriteLine("\t\tKind {0}", s.SourceKind);
                    Log.WriteLine("\t\tDevice {0}", d.Id);
                    Log.WriteLine("\t\t       {0}", d.Name);
                    Log.WriteLine("\t\t       Kind {0}", d.Kind);
                }
                Log.WriteLine("\r\n");
                */
            }
        }

        public static (MediaFrameSourceGroup, MediaFrameSourceInfo) Select(IReadOnlyList<MediaFrameSourceGroup> sources,string groupname,string devicekind)
        {
            var group = sources.Where(x => x.DisplayName.Contains(groupname)).FirstOrDefault();

            if (null == group)
                throw new ApplicationException($"Unable to match source group {groupname}");

            var device = group.SourceInfos.Where(x => x.SourceKind.ToString() == devicekind).FirstOrDefault();

            if (null == device)
                throw new ApplicationException("$Unable to match device {devicekind}");

            return (group, device);
        }

        private MediaCapture capture; // IDisposable
        private MediaFrameReader reader; // IDisposable
        private EventWaitHandle evtframe;

        public async Task Open(MediaFrameSourceGroup frame_group, MediaFrameSourceInfo frame_source_info, bool verbose = false)
        {
            capture = new MediaCapture(); // IDisposable
            MediaCaptureInitializationSettings init = new MediaCaptureInitializationSettings();
            if (verbose)
            {
                Log.WriteLine("Enumerating Frame Source Info");
                Log.WriteLine("Selecting Source");
            }

            init.SourceGroup = frame_group;
            init.SharingMode = MediaCaptureSharingMode.ExclusiveControl;
            //init.SharingMode(MediaCaptureSharingMode::SharedReadOnly);
            //init.MemoryPreference(a.opt.fgpu_only ? MediaCaptureMemoryPreference::Auto : MediaCaptureMemoryPreference::Cpu);
            init.MemoryPreference = MediaCaptureMemoryPreference.Cpu;
            init.StreamingCaptureMode = StreamingCaptureMode.Video;

            if (verbose)
                Log.WriteLine("Enumerating Frame Sources");
            await AsAsync(capture.InitializeAsync(init));

            // This await above has been giving me trouble.
            await Task.Delay(500);

            if (verbose)
                Log.WriteLine("capture initialized");

            var sources = capture.FrameSources;

            if (verbose)
                Log.WriteLine("have frame sources");

            MediaFrameSource source;
            var found = sources.TryGetValue(frame_source_info.Id, out source);
            if (!found)
            {
                throw new ApplicationException(string.Format("can't find source {0}", source));
            }
            if (verbose)
                Log.WriteLine("have frame source that matches chosen source info id");
            // TODO: investigate MediaCaptureVideoProfile, MediaCaptureVideoProfileDescription as a possibly simpler way to do this.
            // NO: MediaCaptureVideoProfile don't have frame reader variant only photo, preview, and redircord.

            // this returns tons of apparent duplicates that can be distinguised using any properties available on the winrt interfaces.
            // i suspect this is because not all the properties on the actual underlying MFVIDEOFORMAT are exposed on the winrt VideoFrameFormat
            var formats = source.SupportedFormats;
            if (verbose)
                Log.WriteLine("have formats");
            MediaFrameFormat format = null;

            if (verbose)
                Log.WriteLine("hunting for format");
            foreach (var f in formats)
            {
                //Log.Write(string.Format("major {0} sub {1} ", f.MajorType, f.Subtype));
                if (f.MajorType == "Video")
                {
                    //Log.Write(string.Format("w {0} h {1} ", f.VideoFormat.Width, f.VideoFormat.Height));
                    if (format == null)
                    {
                        format = f;
                        // Too chatty
                        //Log.Write(" *** Updating Selection *** ");
                    }
                    else
                    {
                        var vf = format.VideoFormat;
                        var new_vf = f.VideoFormat;
                        if (new_vf.Width > vf.Width || new_vf.Height > vf.Height)
                        {  // this will select first of the dupes which hopefully is ok
                            format = f;
                            // Too chatty
                            //Log.Write(" *** Updating Selection *** ");
                        }
                    }
                }
                //Log.WriteLine("");
            }
            if (format == null)
            {
                throw new ApplicationException("Can't find a Video Format");
            }
            if (verbose)
                Log.WriteLine(string.Format("selected videoformat -- major {0} sub {1} w {2} h {3}", format.MajorType, format.Subtype, format.VideoFormat.Width, format.VideoFormat.Height));
            await AsAsync(source.SetFormatAsync(format));
            if (verbose)
                Log.WriteLine("set format complete");

            reader = await AsAsync(capture.CreateFrameReaderAsync(source));
            if (verbose)
                Log.WriteLine("frame reader retrieved\r\n");
            reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            evtframe = new EventWaitHandle(false, EventResetMode.ManualReset);
            reader.FrameArrived += (s,a) => evtframe.Set();

            await AsAsync(reader.StartAsync());
        }

        public async Task<MediaFrameReference> GetFrame()
        {
            var result = reader.TryAcquireLatestFrame();

            while (null == result)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(10));
                result = reader.TryAcquireLatestFrame();
            }

            return result;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    reader?.Dispose();
                    capture?.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Camera() {
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