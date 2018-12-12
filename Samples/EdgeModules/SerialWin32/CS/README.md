# SerialWin32 Azure IoT Edge module

This sample demonstrates how to author a module for Azure IoT Edge to communicate with serial devices on Windows IoT devices.

## Host OS

### Windows IoT Enterprise

These instructions will work on any PC running Windows 10 IoT, including Windows 10 IoT Enterprise, running build 17763.

### Windows IoT Core

These instructions will work on any board running Windows 10 IoT Core build 17763, such as an NXP i.MX6-based board, or a Raspberry Pi 3.

If you don't already have one of these, I recommend the [SolidRun Hummingboard Edge](https://www.solid-run.com/nxp-family/hummingboard/imx6-win-10-iot-core/).

## API Surface

This sample uses Win32 platform APIs to access the serial devices. 
Unfortunately, the standard .NET APIs for serial devices (System.IO.Ports) only work with serial devices which come named "COM{x}".
On Windows IoT Core, serial devices are not so-named.
Using the win32 platform APIs ensures maximum compatibility with all Windows IoT host platforms, and maximum speed.

Furthermore, the current release (17763.55) of Windows IoT SKUs do not contain the components needed to run the System.IO.Ports namespace in containers.
We are currently working to update Windows to fix this problem. 
Once this update is released, for devices which follow the COMx naming pattern, you can use System.IO.Ports in your own solutions.
In the meantime, this is the only available API surface for accessing serial ports from within Windows containers.

## Install Azure IoT Edge

These instructions work with the 1.0.5 release of Azure IoT Edge for Windows.

## Peripheral Hardware

For this sample, obtain an [FTDI Serial TTL-232 cable](https://www.adafruit.com/product/70). Connect the RX and TX lines with a single wire. Plug this into your device.

## Build and Publish the Sample App

Clone or download the sample repo. The first step from there is to publish it from a PowerShell command line, from the SerialWin32/CS directory:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> dotnet publish -r win-arm
Microsoft (R) Build Engine version 15.8.166+gd4e8d81a88 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 53.44 ms for D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\SerialWin32.csproj.
  SerialWin32 -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\bin\Debug\netcoreapp2.1\win-arm\SerialWin32.dll
  SerialWin32 -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\bin\Debug\netcoreapp2.1\win-arm\publish\
```

## ARM32: Copy to device

To build an container module for ARM32 devices, it must be done on an ARM32 device. 
So you'll need to copy the built app to the ARM32 device.
In this case, I first mapped the Q drive to my device.

For X64 containers, you can skip this step and build the container on your development machine.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> robocopy .\bin\Debug\netcoreapp2.1\win-arm\publish\ q:\data\modules\SerialWin32

-------------------------------------------------------------------------------
   ROBOCOPY     ::     Robust File Copy for Windows
-------------------------------------------------------------------------------

  Started : Tuesday, November 20, 2018 8:01:02 AM
   Source : D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\bin\Debug\netcoreapp2.1\win-arm\publish\
     Dest : q:\data\modules\SerialWin32\

    Files : *.*

  Options : *.* /DCOPY:DA /COPY:DAT /R:1000000 /W:30

------------------------------------------------------------------------------
```

While you're here, copy the Dockerfile over to the device as well.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> cp .\Dockerfile.windows-arm32 q:\data\modules\SerialWin32
```

## Create a personal container repository

In order to deploy modules to your device, you will need access to a container respository.  
Refer to the "Create a container registry" section in [Tutorial: Develop a C# IoT Edge module and deploy to your simulated device](https://docs.microsoft.com/en-us/azure/iot-edge/tutorial-csharp-module)

When following the sample, consider "{ACR_*}" notation, to replace those values with the correct values for your container repository.

Be sure to log into the container respository from your device.

```
PS C:\data\modules\SerialWin32> docker login {ACR_NAME}.azurecr.io {ACR_USER} {ACR_PASSWORD}
```

## Containerize the sample app

Build the container on the device. For the remainder of this sample, we will use the environment variable $Container
to refer to the address of our container.

### For ARM32

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> $Container = "{ACR_NAME}.azurecr.io/serialwin32:1.0.0-arm32"

PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker build . -f .\Dockerfile.windows-arm32 -t $Container

Sending build context to Docker daemon  49.54MB

Step 1/5 : FROM mcr.microsoft.com/windows/nanoserver/insider:10.0.17763.55
 ---> 208b271ba3b0
Step 2/5 : ARG EXE_DIR=.
 ---> Using cache
 ---> c2b3fa12c3ea
Step 3/5 : WORKDIR /app
 ---> Using cache
 ---> 7f11b8ea6afc
Step 4/5 : COPY $EXE_DIR/ ./
 ---> ad291836fbe3
Step 5/5 : CMD [ "SerialWin32.exe", "-rtf", "-dPID_6001" ]
 ---> Running in bc730d1a363d
Removing intermediate container bc730d1a363d
 ---> dd5bea81a0bf
Successfully built dd5bea81a0bf
Successfully tagged {ACR_NAME}.azurecr.io/serialwin32:1.0.0-arm32
```

### For X64

The x64 containers can be build directly on the PC, even if you plan to run them on an x64 Windows 10 IoT Core device.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> $Container = "{ACR_NAME}.azurecr.io/serialwin32:1.0.0-x64"

PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker build . -f .\Dockerfile.windows-x64 -t $Container

Sending build context to Docker daemon  81.89MB
Step 1/5 : FROM mcr.microsoft.com/windows/nanoserver/insider:10.0.17763.55
 ---> 91da8a971b53
Step 2/5 : ARG EXE_DIR=bin/Debug/netcoreapp2.1/win-x64/publish
 ---> Running in b537bd4962d6
Removing intermediate container b537bd4962d6
 ---> 6d6281589c30
Step 3/5 : WORKDIR /app
 ---> Running in b8f3943ab2e5
Removing intermediate container b8f3943ab2e5
 ---> 37f5488097e5
Step 4/5 : COPY $EXE_DIR/ ./
 ---> 49f265682955
Step 5/5 : CMD [ "SerialWin32.exe", "-rte", "-dPID_6001" ]
 ---> Running in 1aedd449ffa4
Removing intermediate container 1aedd449ffa4
 ---> d6cbd51600e3
Successfully built d6cbd51600e3
Successfully tagged {ACR_NAME}.azurecr.io/serialwin32:1.0.0-x64
```

## Test the sample app on the device

At this point, we'll want to run the container locally to ensure that it is able to find and talk to our peripheral.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker run --device "class/86E0D1E0-8089-11D0-9CE4-08003E301F73" --isolation process $Container SerialWin32.exe

SerialWin32 1.0.0.0
  -h, --help                 show this message and exit
  -l, --list                 list available devices and exit
  -d, --device=ID            the ID of device to connect
  -r, --receive              receive and display packets
  -t, --transmit             transmit packets (combine with -r for loopback)
  -c, --config               display device configuration
  -e, --edge                 transmit through azure edge  
Available devices:
\\?\ACPI#QCOM2424#2#{86e0d1e0-8089-11d0-9ce4-08003e301f73}
\\?\FTDIBUS#VID_0403+PID_6001+A403A7H4A#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}
\\?\ACPI#QCOM2424#1#{86e0d1e0-8089-11d0-9ce4-08003e301f73}
```

This will show that the application is able to run through the container, and list the available devices. 
Notice that we are overriding the entry point from the command line.

Now let's pick a device and ensure we can open it:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker run --device "class/86E0D1E0-8089-11D0-9CE4-08003E301F73" --isolation process $Container SerialWin32.exe -c -dPID_6001

11/20/2018 8:11:29 AM Connecting to device \\?\FTDIBUS#VID_0403+PID_6001+A403A7H4A#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}...
=====================================
DCBlength: 0x1C
BaudRate: 0x1C200
flags: 0x1011
wReserved: 0x0
XonLim: 0x800
XoffLim: 0x200
ByteSize: 0x7
Parity: 0x2
StopBits: 0x0
XonChar: 0x11
XoffChar: 0x13
ErrorChar: 0x0
EofChar: 0x0
EvtChar: 0x0
wReserved1: 0x0
ReadIntervalTimeout: 0xA
ReadTotalTimeoutMultiplier: 0x0
ReadTotalTimeoutConstant: 0x0
WriteTotalTimeoutMultiplier: 0x0
WriteTotalTimeoutConstant: 0x0
=====================================
```

## Push the container

Now, we push the container into the repository which we built earlier. At this point, the container image is waiting for us to deploy.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker push $Container

The push refers to repository [{ACR_NAME}.azurecr.io/serialwin32]
bf4863b963b0: Preparing
3ed1316f55e1: Preparing
b4d9f6916bae: Preparing
6d34deee1fa7: Preparing
79c160bd8d82: Preparing
79c160bd8d82: Skipped foreign layer
b4d9f6916bae: Mounted from serial-module
bf4863b963b0: Pushed
6d34deee1fa7: Mounted from serial-module
3ed1316f55e1: Pushed
1.0.0-arm32: digest: sha256:9593c87a18198915118ecbdc7f5a308dbe34a15ef898bac3fb5d06730ae6d30a size: 1464
```

## Edit the deployment.json file

In the repo, you will find separate deployment.{arch}.json files for each architecture.
Choose the deployment file corresponding to your deployment atchitecture, then fill in the details for your container image.
Search for "{ACR_*}" and replace those values with the correct values for your container repository.
The ACR_IMAGE must exactly match what you pushed, e.g. jcoliz.azurecr.io/serial-module:1.0.0-arm32

```
    "$edgeAgent": {
      "properties.desired": {
        "runtime": {
          "settings": {
            "registryCredentials": {
              "jiriatest": {
                "username": "d3e6e3bc-2e38-4887-9073-2cf796462b15",
                "password": "71181f94-a9b9-4b98-96a8-01c4ae8dff94",
                "address": "edgeshared.azurecr.io"
              },
              "{ACR_NAME}": {
                "username": "{ACR_USER}",
                "password": "{ACR_PASSWORD}",
                "address": "{ACR_NAME}.azurecr.io"
              }
            }
          }
        }
...
        "modules": {
          "serial": {
            "settings": {
              "image": "{ACR_IMAGE}",
              "createOptions": "{\"HostConfig\":{\"Devices\":[{\"CgroupPermissions\":\"\",\"PathInContainer\":\"\",\"PathOnHost\":\"class/86E0D1E0-8089-11D0-9CE4-08003E301F73\"}],\"Isolation\":\"Process\"}}"
            }
          }

```

## Deploy

Back on your PC, you can now deploy this deployment.json file to your device.
For reference, please see [Deploy Azure IoT Edge modules from Visual Studio Code](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-vscode)

## Verify

Using the Azure IoT Edge extension for Visual Studio Code, you can select your device and choose "Start Monitoring D2C Message". You should see this:

```
[IoTHubMonitor] [4:04:43 PM] Message received from [jcoliz-preview/serialwin32]:
{
  "machine": {
    "temperature": 56.32,
    "pressure": 99.01
  },
  "ambient": {
    "temperature": 95.51,
    "humidity": 24
  },
  "timeCreated": "2018-11-19T16:04:43.0331117-08:00"
}
[IoTHubMonitor] [4:04:44 PM] Message received from [jcoliz-preview/serialwin32]:
{
  "machine": {
    "temperature": 14.47,
    "pressure": 33.99
  },
  "ambient": {
    "temperature": 83.95,
    "humidity": 26
  },
  "timeCreated": "2018-11-19T16:04:44.0555983-08:00"
}
```

From a command prompt on the device, you can also check the logs for the module itself.

First, find the module container:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker ps

CONTAINER ID        IMAGE                                                                           COMMAND                  CREATED              STATUS              PORTS                                                                  NAMES
b4107d30a29d        {ACR_NAME}.azurecr.io/serialwin32:1.0.2-arm32                                       "SerialWin32.exe -rt…"   About a minute ago   Up About a minute                                                                          serialwin32
56170371f8f5        edgeshared.azurecr.io/microsoft/azureiotedge-hub:1809_insider-windows-arm32     "dotnet Microsoft.Az…"   3 days ago           Up 6 minutes        0.0.0.0:443->443/tcp, 0.0.0.0:5671->5671/tcp, 0.0.0.0:8883->8883/tcp   edgeHub
27c147e5c760        edgeshared.azurecr.io/microsoft/azureiotedge-agent:1809_insider-windows-arm32   "dotnet Microsoft.Az…"   3 days ago           Up 7 minutes                                                                               edgeAgent
```

Then, use the ID for the serialwin32 container to check the logs

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker logs b4107d30a29d
11/20/2018 8:22:41 AM Connecting to device \\?\FTDIBUS#VID_0403+PID_6001+A403A7H4A#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}...
11/20/2018 8:22:55 AM IoT Hub module client initialized.
11/20/2018 8:22:55 AM Async Read 1 Started
11/20/2018 8:22:57 AM Write 1 Started
11/20/2018 8:22:57 AM Write 1 Completed. Wrote 30 bytes: "00001/005.53,069.14/002.80,024"
11/20/2018 8:22:57 AM Async Read 1 Completed. Received 11 bytes: "00001/005.5                   "
IndexOutOfRangeException: Index was outside the bounds of the array.
IndexOutOfRangeException: Index was outside the bounds of the array.
11/20/2018 8:22:57 AM Async Read 2 Started
11/20/2018 8:22:57 AM Async Read 2 Completed. Received 19 bytes: "3,069.14/002.80,024           "
FormatException: Input string was not in a correct format.
11/20/2018 8:22:57 AM Async Read 3 Started
11/20/2018 8:22:58 AM Write 2 Started
11/20/2018 8:22:58 AM Write 2 Completed. Wrote 30 bytes: "00002/028.58,068.74/042.45,026"
11/20/2018 8:22:58 AM Async Read 3 Completed. Received 30 bytes: "00002/028.58,068.74/042.45,026"
11/20/2018 8:22:58 AM SendEvent: [{"machine":{"temperature":28.58,"pressure":68.74},"ambient":{"temperature":42.45,"humidity":26},"timeCreated":"2018-11-20T08:22:58.0952836-08:00"}]
11/20/2018 8:22:59 AM Write 3 Started
11/20/2018 8:22:59 AM Write 3 Completed. Wrote 30 bytes: "00003/024.49,098.45/029.64,026"
11/20/2018 8:22:59 AM Async Read 4 Started
11/20/2018 8:22:59 AM Async Read 4 Completed. Received 30 bytes: "00003/024.49,098.45/029.64,026"
11/20/2018 8:22:59 AM SendEvent: [{"machine":{"temperature":24.49,"pressure":98.45},"ambient":{"temperature":29.64,"humidity":26},"timeCreated":"2018-11-20T08:22:59.1249324-08:00"}]
```