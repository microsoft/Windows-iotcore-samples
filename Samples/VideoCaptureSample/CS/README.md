# Video Capture Sample

Initialize a video capture device, record video to a file, preview video feed, and playback recorded video.

This is a headed sample.  To better understand what headed mode is and how to configure your device to be headed, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).

### Connecting your Video Capture device

You'll need:

* <a name="USB_WebCam"></a>A USB web cam (Example: [Microsoft Life Cam](http://www.microsoft.com/hardware/en-us/p/lifecam-hd-3000))

Connect the web cam to one of USB ports on the IoT Device

### Deploy your app

If you're building for Minnowboard Max, select `x86` as the architecture. If you're building for Raspberry Pi 2 or 3 or DragonBoard , select `ARM`.

**DragonBoard only:** Before deploying this app, disable the on-board mic and audio drivers. This is required on every reboot when running this app
``` xml
DragonBoard - Commands to disable audio and mic drivers:

devcon remove AUDD\QCOM2451
devcon remove ADCM\QCOM242E
```

Select **Remote Machine** to point to IoT device and hit F5 to deploy to your device. Go back to the basic 'Hello World' [sample](/Samples/HelloWorld) if you need guidance.

### Test your app

The sample app when deployed displays 3 buttons `Start Capturing`, `End Capturing`and `Play Captured Video`. Below is a description of the actions available when the buttons are clicked.

### Start Capturing:

* Start video recording to cameraCapture.wmv file in Videos Library folder.
* Preview stream will appear under Video Preview Window section of the canvas.

### End Capturing:

* Stops preview and stops capturing video to cameraCapture.wmv file.

### Play Captured Video:

* Reads recorded video data from cameraCapture.wmv file.
* View stream will appear under Video Review Window section of the canvas.

**NOTE:** In order to hear the audio, an audio device (ex: earphones) must be connected to the analog audio output on Raspberry Pi2. On MinnowBoard Max, audio output is available via HDMI

Congratulations! You created your first video recording app.

### Let's look at the code

The code for this sample uses the [Windows.Media.Capture](https://msdn.microsoft.com/en-us/library/windows/apps/windows.media.capture.aspx) namespace.

**MediaCapture** class will be used to enumerate, connect to, and perform actions using the video capture or web camera connected to the device.

For accessing the web cam, the microphone, and the default storage folders, you must add the following capabilities to the **Package.appxmanifest** file in your project.


**NOTE:** You can add capabilities directly by opening the **Package.appxmanifest** file in an XML editor (Right Click on the file -> Open with -> XML (Text) Editor) and adding the capabilities below:

``` xml
 <Capabilities>
   <uap:Capability Name="videosLibrary" />
   <DeviceCapability Name="webcam" />
   <DeviceCapability Name="microphone" />
 </Capabilities>
```

## Initialize MediaCapture object

When the MainPage is being initialized, sample enumerates all available Video Capture devices.
Then it initalizes **MediaCapture** object that can be configured to capture video and/or audio only. In the sample, we use AudioAndVideo capture mode.


```csharp
private async void EnumerateCameras()
{
    var devices = await Windows.Devices.Enumeration.DeviceInformation.FindAllAsync(Windows.Devices.Enumeration.DeviceClass.VideoCapture);
    deviceList = new List<Windows.Devices.Enumeration.DeviceInformation>();

    if (devices.Count > 0)
    {
        for(var i = 0; i < devices.Count; i++)
        {
            deviceList.Add(devices[i]);
        }

        InitCaptureSettings();
        InitMediaCapture();
    }
}

private void InitCaptureSettings()
{
    captureInitSettings = new Windows.Media.Capture.MediaCaptureInitializationSettings();
    captureInitSettings.AudioDeviceId = "";
    captureInitSettings.VideoDeviceId = "";
    captureInitSettings.StreamingCaptureMode = Windows.Media.Capture.StreamingCaptureMode.AudioAndVideo;
    captureInitSettings.PhotoCaptureSource = Windows.Media.Capture.PhotoCaptureSource.VideoPreview;

    if (deviceList.Count > 0)
    {
        captureInitSettings.VideoDeviceId = deviceList[0].Id;
    }
}

private async void InitMediaCapture()
{
    mediaCapture = new Windows.Media.Capture.MediaCapture();
    await mediaCapture.InitializeAsync(captureInitSettings);

    // ...

	// wire to preview XAML element
    capturePreview.Source = mediaCapture;

    // ...
}
```

### Start Capture

Once MediaCapture object has been initialized, clicking on `Start Capturing` begins media capture to a file in Videos Library folder for DefaultAccount. Sample also enables preview that is wired to a **CaptureElement** XAML element.

```csharp
private async Task StartMediaCaptureSession()
{
    await StopMediaCaptureSession();

    var storageFile = await Windows.Storage.KnownFolders.VideosLibrary.CreateFileAsync("cameraCapture.wmv", Windows.Storage.CreationCollisionOption.GenerateUniqueName);
    fileName = storageFile.Name;

    await mediaCapture.StartRecordToStorageFileAsync(profile, storageFile);
    await mediaCapture.StartPreviewAsync();
    isRecording = true;
}
```

### End Capture

To end capturing and preview, clicking on the `End Capture` button stops preview and stops recording to a file. It may take a few seconds to flush all data to the video file before it can be played back.

```csharp
private async Task StopMediaCaptureSession()
{
    if (isRecording)
    {
        await mediaCapture.StopPreviewAsync();
        await mediaCapture.StopRecordAsync();
        isRecording = false;
    }
}
```

### Play Captured Video

After a file has been recorded, it can be played back by pressing `Play Captured VIdeo` button. This sets up a stream from the file to **MediaElement** XAML control.

```csharp
private async void playVideo(object sender, RoutedEventArgs e)
{
    Windows.Storage.StorageFile storageFile = await Windows.Storage.KnownFolders.VideosLibrary.GetFileAsync(fileName);

    using (var stream = await storageFile.OpenAsync(Windows.Storage.FileAccessMode.Read))
    {
        if (null != stream)
        {
            media.Visibility = Visibility.Visible;
            media.SetSource(stream, storageFile.ContentType);
            media.Play();
        }
    }
}
```

### Video Preview and Playback controls

This section describes how we render preview and playback of video content in MainPage.
The preview is rendered in **CaptureElement** control that has its Source pointing to **MediaPlayback** object.

``` xml
<CaptureElement Name="capturePreview" Height="120" Margin="10,0,0,10" Width="120"/>
```

The playback is rendered in **MediaElement** control that has its Source pointing for a file stream from the captured video data file.
``` xml
<MediaElement Name="media"
              AutoPlay="True"
              AreTransportControlsEnabled="False"
              Height="120"
              Width="120"
              Margin="0,10,0,10"
              />
```


## To summarize:

* First, we create a **MediaCapture** object to initialize the Video Capture device like a web cam for VideoAndAudio

* Based on user input, we initialize camera preview and record a video to a file, and playback the recording.

* Media files are stored under Videos Library.
