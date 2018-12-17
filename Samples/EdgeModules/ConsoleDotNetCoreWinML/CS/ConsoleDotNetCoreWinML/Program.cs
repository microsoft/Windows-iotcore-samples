using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.AI.MachineLearning;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Capture;
using Windows.Media.Capture.Frames;
using Windows.Storage;

namespace ConsoleDotNetCoreWinML
{
    class AsyncHelper {
        // Work around this problem:
        // https://github.com/Microsoft/dotnet/issues/590
        // https://github.com/dotnet/corefx/issues/22789
        public static async Task<T> SyncFromAsync<T>(IAsyncOperation<T> op, string dbgtag)
        {

            T result = default(T);
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
                result = op.GetResults();
            }

            return result;
        }
        public static async Task SyncFromAsync(IAsyncAction op, string dbgtag)
        {
            using (var AsyncMeSemaphore = new SemaphoreSlim(0, 1))
            {
                op.Completed += (o, s) =>
                {
                    AsyncMeSemaphore.Release();
                };
                // in case the op completes before the handler got connected we must check
                // status and complete things before waiting
                if (op.Status == AsyncStatus.Completed)
                {
                    AsyncMeSemaphore.Release();
                }
                await AsyncMeSemaphore.WaitAsync();
            }

            return;
        }
    }

    class Program
    {

        private static async Task<Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>> EnumFrameSourcesAsync()
        {
            MediaFrameSourceInfo result_info = null;
            MediaFrameSourceGroup result_group = null;
            var sourcegroups = await AsyncHelper.SyncFromAsync(MediaFrameSourceGroup.FindAllAsync(), "sourcegroups");
            Log.WriteLine("found {0} Source Groups", sourcegroups.Count);
            foreach (var g in sourcegroups)
            {
                var sourceinfos = g.SourceInfos;
                Log.WriteLine("Source Group {0}", g.Id);
                Log.WriteLine("             {0}", g.DisplayName);
                Log.WriteLine("             with {0} Sources:", sourceinfos.Count);
                foreach (var s in sourceinfos)
                {
                    var d = s.DeviceInformation;
                    Log.WriteLine("\t{0}", s.Id);
                    Log.WriteLine("\t\tKind {0}", s.SourceKind);
                    Log.WriteLine("\t\tDevice {0}", d.Id);
                    Log.WriteLine("\t\t       {0}", d.Name);
                    Log.WriteLine("\t\t       Kind {0}", d.Kind);
                    if (result_info == null)
                    {
                        result_info = s; // for now just pick the first thing we find
                    }
                }
                Log.WriteLine("\r\n");
                if (result_group == null)
                {
                    result_group = g; // for now just pick the first thing we find
                }
            }
            return new Tuple<MediaFrameSourceGroup, MediaFrameSourceInfo>(result_group, result_info);
        }
        static async Task<Tuple<MediaFrameReader, EventWaitHandle>> GetFrameReaderAsync()
        {
            MediaCapture capture = new MediaCapture();
            MediaCaptureInitializationSettings init = new MediaCaptureInitializationSettings();
            Log.WriteLine("Enumerating Frame Source Info");
            var (frame_group, frame_source_info) = await EnumFrameSourcesAsync();
            Log.WriteLine("Selecting Source");

            init.SourceGroup = frame_group;
            init.SharingMode = MediaCaptureSharingMode.ExclusiveControl;
            //init.SharingMode(MediaCaptureSharingMode::SharedReadOnly);
            //init.MemoryPreference(a.opt.fgpu_only ? MediaCaptureMemoryPreference::Auto : MediaCaptureMemoryPreference::Cpu);
            init.MemoryPreference = MediaCaptureMemoryPreference.Cpu;
            init.StreamingCaptureMode = StreamingCaptureMode.Video;
            Log.WriteLine("Enumerating Frame Sources");
            await AsyncHelper.SyncFromAsync(capture.InitializeAsync(init), "capture init");
            Log.WriteLine("capture initialized");
            var sources = capture.FrameSources;
            Log.WriteLine("have frame sources");
            MediaFrameSource source;
            var found = sources.TryGetValue(frame_source_info.Id, out source);
            if (!found)
            {
                throw new ApplicationException(string.Format("can't find source {0}", source));
            }
            Log.WriteLine("have frame source that matches chosen source info id");
            // MediaCaptureVideoProfile doesn't have frame reader variant only photo, preview, and record.
            // so we will enumerate and select instead of just declaring what we want and having the system
            // give us the closest match
            var formats = source.SupportedFormats;
            Log.WriteLine("have formats");
            MediaFrameFormat format = null;

            Log.WriteLine("hunting for format");
            foreach (var f in formats)
            {
                Log.Write(string.Format("major {0} sub {1} ", f.MajorType, f.Subtype));
                if (f.MajorType == "Video")
                {
                    Log.Write(string.Format("w {0} h {1} ", f.VideoFormat.Width, f.VideoFormat.Height));
                    if (format == null)
                    {
                        format = f;
                        Log.Write(" *** Updating Selection *** ");
                    }
                    else
                    {
                        var vf = format.VideoFormat;
                        var new_vf = f.VideoFormat;
                        if (new_vf.Width > vf.Width || new_vf.Height > vf.Height)
                        {  // this will select first of the dupes which hopefully is ok
                            format = f;
                            Log.Write(" *** Updating Selection *** ");
                        }
                    }
                }
                Log.WriteLine("");
            }
            if (format == null)
            {
                throw new ApplicationException("Can't find a Video Format");
            }
            Log.WriteLine(string.Format("selected videoformat -- major {0} sub {1} w {2} h {3}", format.MajorType, format.Subtype, format.VideoFormat.Width, format.VideoFormat.Height));
            await AsyncHelper.SyncFromAsync(source.SetFormatAsync(format), "set format");
            Log.WriteLine("set format complete");
            var reader = await AsyncHelper.SyncFromAsync(capture.CreateFrameReaderAsync(source), "reader");
            Log.WriteLine("frame reader retrieved\r\n");
            reader.AcquisitionMode = MediaFrameReaderAcquisitionMode.Realtime;
            var evtframe = new EventWaitHandle(false, EventResetMode.ManualReset);
            reader.FrameArrived += (MediaFrameReader sender, MediaFrameArrivedEventArgs args) => evtframe.Set();
            return new Tuple<MediaFrameReader, EventWaitHandle>(reader, evtframe);
        }

        static async Task<Tuple<UInt64, double, DateTime, string>> FrameProcessingAsync(Model model, MediaFrameReference frame, UInt64 totalFrame, UInt64 oldFrame, double oldFps, DateTime oldFpsT0, string oldLabel, AzureConnection azure)
        {
            UInt64 currentFrame = oldFrame;
            double fps = oldFps;
            DateTime fpsT0 = oldFpsT0;
            string prevLabel = oldLabel;
            string correlationId = String.Format("{0}", totalFrame);
            var r = await model.EvaluateAsync(frame, correlationId);
            if (!r._result.Succeeded)
            {
                Log.WriteLineUp3Home("eval failed {0}", r._result.ErrorStatus);
            }
            else
            {
                Log.WriteLineUp3Home("eval succeeded");
            }

            var fps_interval = DateTime.Now - fpsT0;
            if (fps_interval.Seconds > 10)
            {
                fps = currentFrame * 1.0 / fps_interval.Seconds;
                currentFrame = 0;
                fpsT0 = DateTime.Now;
            }
            Log.WriteLineHome("fps {0}", fps.ToString("0.00"));


            string label = r._output.classLabel;
            var most = r.MostProbable;
            if (label != most.Key)
            {
                throw new ApplicationException(string.Format("Output Feature Inconsistency model output label 0 '{0}' and label 1 '{1}'", label, most.Key));
            }
            Log.WriteLine("{1}{2} with probability {3}", Log.fmt_output_success, most.Key, Log.fmt_output_default, most.Value.ToString("0.0000"));
            // we would like to apply a confidence threshold but with these models garbage always ends up high confidence something 
            if (prevLabel == null || prevLabel != label)
            {
                prevLabel = label;
                azure.UpdateObject(label);

            }

            model.Clear(correlationId);
            return new Tuple<UInt64, double, DateTime, string>(currentFrame, fps, fpsT0, prevLabel);
        }
        static async Task CameraProcessingAsync(Model model, MediaFrameReader reader, EventWaitHandle evtframe, AzureConnection azure)
        {
            var fps_t0 = DateTime.Now;
            string prev_label = null;

            double fps = 0.0;
            for (UInt64 total_frame = 0, current_frame = 0; ; ++total_frame)
            {
                if (Model.Full)
                {
                    evtframe.WaitOne();
                    evtframe.Reset();
                }
                //auto input_feature{ImageFeatureValue::CreateFromVideoFrame(vf.get())};
                var frame = reader.TryAcquireLatestFrame();
                if (frame == null)
                {
                    // assume 60 fps, wait about half a frame for more input.  
                    // in the unlikely event that eval is faster than capture this should be done differently
                    Thread.Sleep(10);
                    continue;
                }
                ++current_frame;
                //int oldFrame, double oldFps, DateTime oldFpsT0, string oldLabel)
                (current_frame, fps, fps_t0, prev_label) = await FrameProcessingAsync(model, frame, total_frame, current_frame, fps, fps_t0, prev_label, azure);
            }
        }

        static async Task<int> MainAsync(string[] args)
        {
            //Log.WriteLine("pause...");
            //var x = Console.ReadLine();
            Log.WriteLine("Starting async...");
            Model model = null;
            AzureConnection connection = null;
            MediaFrameReader reader = null;
            EventWaitHandle evtFrame = null;

            await Task.WhenAll(
                Task.Run(async () =>
                    model = await Model.CreateModelAsync(
                        Directory.GetCurrentDirectory() + "\\resources\\office_fruit_coreml.onnx")),
                Task.Run(async () =>
                    connection = await AzureConnection.CreateAzureConnectionAsync()),
                Task.Run(async () => {
                    (reader, evtFrame) = await GetFrameReaderAsync();
                    await AsyncHelper.SyncFromAsync(reader.StartAsync(), "reader start");
                    })
                );

            Log.WriteLine("Model loaded, Azure Connection created, and FrameReader Started\n\n\n\n");

            await CameraProcessingAsync(model, reader, evtFrame, connection);

            return 0;
        }
        static void Usage()
        {
            Log.Enabled = true;
            Log.WriteLineError("{0} {-l}", "ConsoleDotNetCoreWinML");
            Log.WriteLineError("\t-l print log info to console");
            Environment.Exit(1);
        }
        static void ProcessArguments(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].ToLowerInvariant() == "-l")
                {
                    Log.Enabled = true;
                }
            }
        }
        static int Main(string[] args)
        {
            Log.WriteLine("Starting...");
            int rc = 0;
            try
            {
                ProcessArguments(args);
                Task.WaitAll(Task.Run(async () =>
                    rc = await MainAsync(args)));
            } catch(Exception e)
            {                
                Log.WriteLineError("app failed {0}", e.ToString());
                rc = 1;
            }
            Log.WriteLine("Complete....");
            Console.Out.Flush();
            return rc;
        }
    }
}
