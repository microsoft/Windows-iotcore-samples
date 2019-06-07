//
// Copyright (c) Microsoft. All rights reserved.
//

using EdgeModuleSamples.Common;
using EdgeModuleSamples.Common.Logging;
using AudioLoopback;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices;
using Windows.Devices.Enumeration;
using Windows.Media;
using Windows.Media.MediaProperties;
using Windows.Media.Audio;
using Windows.Media.Devices;


namespace AudioLoopback
{

    public abstract class AudioBaseDevice<AudioDeviceT> : IDisposable where AudioDeviceT : IDisposable, IAudioNode
    {
        AudioDeviceT _device;
        AudioGraph _graph;
        // not same as other SPB, private ctor, public static cread to allow multiple AudioController 
        // Audiocontroller class only has getdefault(). there is no getdeviceselector(friendlynamestring)
        public AudioBaseDevice(AudioGraph g)
        {
            _graph = g;
            Log.WriteLine("Audio Base Device Initializing ctor complete");
        }
        public AudioBaseDevice()
        {
            Log.WriteLine("Audio Base Device default ctor complete");
        }
        ~AudioBaseDevice()
        {
            Dispose(false);
        }
        public AudioDeviceT Device
        {
            get
            {
                return _device;
            }
            protected set { _device = value; }
        }
        public AudioGraph Graph
        {
            get
            {
                return _graph;
            }
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (Device != null)
                {
                    Device.Dispose();
                    Device = default(AudioDeviceT);
                }
            }
        }
        public static async Task<DeviceInformation> SelectAsync(string selector, string name)
        {
            var dis = await AsyncHelper.AsAsync(Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(selector));
            return dis.Where(d => d.Name.Contains(name)).FirstOrDefault();
        }
        public static async Task ListDevicesAsync()
        {
            DeviceInformationCollection inputs = null;
            DeviceInformationCollection outputs = null;
            Task.WaitAll(
                Task.Run(async () => inputs =
                    await AsyncHelper.AsAsync(DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector()))),
                Task.Run(async () => outputs =
                    await AsyncHelper.AsAsync(DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector())))
            );

            Log.WriteLine("Audio Input Devices:");

            foreach (var i in inputs)
            {
                Log.WriteLine($"\t{i.Id}: {i.Name}");
            }
            Log.WriteLine("Audio Output Devices:");
            foreach (var o in outputs)
            {
                Log.WriteLine($"\t{o.Id}: {o.Name}");
            }
            await Task.CompletedTask;
        }
    }

    public class AudioInputDevice : AudioBaseDevice<AudioDeviceInputNode>
    {
        public AudioInputDevice(AudioGraph g) : base(g)
        {
        }
        public static async Task<DeviceInformation> SelectAsync(string name)
        {
            var s = MediaDevice.GetAudioCaptureSelector();
            var di = default(DeviceInformation);
            if (name != null)
            {
                await SelectAsync(s, name);
            }
            else
            {
                await Task.CompletedTask;
            }
            if (di == default(DeviceInformation))
            {
                s = MediaDevice.GetDefaultAudioCaptureId(0);
                di = await AsyncHelper.AsAsync(Windows.Devices.Enumeration.DeviceInformation.CreateFromIdAsync(s));
            }
            return di;
        }
        public async Task InitializeAsync(DeviceInformation di, AudioEncodingProperties settings)
        {
            Log.WriteLine($"attempting to create input device {di.Id}");

            var result = await AsyncHelper.AsAsync(Graph.CreateDeviceInputNodeAsync(Windows.Media.Capture.MediaCategory.Speech, settings, di));
            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new ApplicationException($"audio input device {di.Id}:{di.Name} create failed. status = {result.Status} ex = {result.ExtendedError.ToString()}");
            }
            Device = result.DeviceInputNode;
        }
        public void Connect<AudioDeviceT>(AudioBaseDevice<AudioDeviceT> downstream) where AudioDeviceT : IDisposable, IAudioNode
        {
            Device.AddOutgoingConnection(downstream.Device);
        }

    }

    public class AudioOutputDevice : AudioBaseDevice<AudioDeviceOutputNode>
    {
        public AudioOutputDevice(AudioGraph g) : base(g)
        {
        }
        public static async Task<DeviceInformation> SelectAsync(string name)
        {
            var s = MediaDevice.GetAudioRenderSelector();
            var di = default(DeviceInformation);
            if (name != null)
            {
                await SelectAsync(s, name);
            }
            else
            {
                await Task.CompletedTask;
            }
            if (di == default(DeviceInformation))
            {
                s = MediaDevice.GetDefaultAudioRenderId(0);
                di = await AsyncHelper.AsAsync(Windows.Devices.Enumeration.DeviceInformation.CreateFromIdAsync(s));
            }
            return di;
        }
        public async Task InitializeAsync()
        {
            var result = await AsyncHelper.AsAsync(Graph.CreateDeviceOutputNodeAsync());
            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                throw new ApplicationException($"audio output device create failed. status = {result.Status} ex = {result.ExtendedError.ToString()}");
            }
            Device = result.DeviceOutputNode;
        }
    }
}
