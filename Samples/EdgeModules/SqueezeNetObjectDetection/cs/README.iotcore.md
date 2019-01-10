# Build & Run on IoT Core

The primary instructions for this sample describe how to build and publish the sample to target a machine running Windows 10 IoT Enterprise (or Windows 10 to test).

These instructions describe the steps needed to deploy the sample to a target machine running Windows 10 IoT Core.

## Prerequisites

Same as for the primary instructions, except:

* Target Hardware: A [Minnowboard Turbot](https://minnowboard.org/minnowboard-turbot/) running [Windows 10 IoT Core - Build 17763](https://developer.microsoft.com/en-us/windows/iot). Currently, the sample runs only on x64 architecture. Future releases will include support for arm32 architecture.

### Insider Knowledge

Unfortunately, this sample cannot be reproduced on IoT Core using publicly released tools, until the following PR's are upstreamed and released:

* [IoT Edge PR 670](https://github.com/Azure/iotedge/pull/670). Without this change, IoT Edge fills up the very small MainOS partition on IoT Core devices. To work around, I have installed docker manually, and reconfigured Edge to use this manually-installed docker.

## Complete the primary instructions first

Please complete the primary instructions first, to gain familiarity with the whole process running from your PC first. Then, complete these separate steps for an IoT Core solution.  

## Copy published files to target device

IoT Core container images must be built on an IoT Core device. To enable this, we will copy the 'publish' folder over to our device. 
In this case, I have mapped the Q: drive on my development PC to the C: drive on my IoT Core device.

```
PS C:\Windows-iotcore-samples\Samples\EdgeModules\SqueezeNetObjectDetection\cs> robocopy bin\Debug\netcoreapp2.1\win-x64\publish\ q:\data\modules\squeezenet

-------------------------------------------------------------------------------
   ROBOCOPY     ::     Robust File Copy for Windows
-------------------------------------------------------------------------------

  Started : Friday, December 21, 2018 4:20:48 PM
   Source : C:\Windows-iotcore-samples\Samples\EdgeModules\SqueezeNetObjectDetection\cs\bin\Debug\netcoreapp2.1\win-x64\publish\
     Dest : q:\data\modules\squeezenet\

    Files : *.*

  Options : *.* /DCOPY:DA /COPY:DAT /R:1000000 /W:30

------------------------------------------------------------------------------
```

## Test the sample on the target device

Following the same approach as above, run the app on the target device to ensure you have the correct camera there, and it's working on that device.

```
[192.168.1.120]: PS C:\data\modules\squeezenet> .\SqueezeNetObjectDetection.exe --list
Found 1 Cameras
Microsoft® LifeCam Studio(TM)

[192.168.1.120]: PS C:\data\modules\squeezenet> .\SqueezeNetObjectDetection.exe --model=SqueezeNet.onnx --device=LifeCam
Loading modelfile 'SqueezeNet.onnx' on the 'default' device...
...OK 1079 ticks
Retrieving image from camera...
...OK 766 ticks
Running the model...
...OK 625 ticks
12/28/2018 12:13:05 PM Sending: {"results":[{"label":"coffee mug","confidence":0.99733692407608032},{"label":"cup","confidence":0.0024446924217045307},{"label":"water jug","confidence":8.2805654528783634E-06}]}
```

## Containerize the sample app

Build the container on the device. For the remainder of this sample, we will use the environment variable $Container
to refer to the address of our container.

```
[192.168.1.120]: PS C:\Data\modules\squeezenet> $Container = "{ACR_NAME}.azurecr.io/squeezenet:1.0.0-x64-iotcore"

[192.168.1.120]: PS C:\Data\modules\squeezenet> docker build . -f Dockerfile.iotcore -t $Container
Sending build context to Docker daemon  81.63MB

Step 1/5 : FROM mcr.microsoft.com/windows/iotcore:1809
 ---> 2d0e5d769eb2
Step 2/5 : ARG EXE_DIR=.
 ---> Using cache
 ---> e56aef21bf6c
Step 3/5 : WORKDIR /app
 ---> Using cache
 ---> 5b66e01e041a
Step 4/5 : COPY $EXE_DIR/ ./
 ---> 3798927f4eaa
Step 5/5 : CMD [ "SqueezeNetObjectDetection.exe", "-mSqueezeNet.onnx", "-dLifeCam", "-ef" ]
 ---> Running in c4f09d9edc5b
Removing intermediate container c4f09d9edc5b
 ---> e7ab7ea5bd16
Successfully built e7ab7ea5bd16
Successfully tagged {ACR_NAME}.azurecr.io/squeezenet:1.0.0-x64-iotcore
```

## Continue as before

The remainder of the instructions apply whether the container was built on an IoT Core device or on a PC. In the primary instructions, continue from the step labeled "Push the container".
