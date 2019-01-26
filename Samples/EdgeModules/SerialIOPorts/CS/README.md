# USB Serial Access in an Azure IoT Edge module on Windows

This document will walk you through creating a module for Azure IoT Edge on Windows 10 IoT Enterprise which accesses a USB Serial device.

## API Surface

This sample uses standard .NET Core System.IO.Ports APIs to access the serial devices. 
These APIs only work with serial devices which come named "COM{x}".
Consequently, this approach is relevant on Windows 10 IoT Enterprise, but not Windows 10 IoT Core.

## Install Azure IoT Edge

These instructions work with the 1.0.5 release of [Azure IoT Edge for Windows](https://docs.microsoft.com/en-us/azure/iot-edge/).

## Host Hardware & OS

These instructions will work on any PC running Windows 10, including Windows 10 IoT Enterprise. 
The PC must be running build 17763 of Windows 10, and must be 17763.253 or higher.

## Peripheral Hardware

Obtain an [FTDI Serial TTL-232 cable](https://www.adafruit.com/product/70). Connect the RX and TX lines with a single wire. Plug this into your device.

## Build and Publish the Sample App

Clone or download the sample repo. The first step from there is to publish it from a PowerShell command line, from the SerialIOPorts/CS directory.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> dotnet publish -r win-x64
Microsoft (R) Build Engine version 15.8.166+gd4e8d81a88 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 53.44 ms for D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS\SerialIOPorts.csproj.
  SerialIOPorts -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS\bin\Debug\netcoreapp2.1\win-x64\SerialIOPorts.dll
  SerialIOPorts -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS\bin\Debug\netcoreapp2.1\win-x64\publish\
```

## Create a personal container repository

In order to deploy modules to your device, you will need access to a container respository. 
Refer to [Quickstart: Create a private container registry using the Azure portal](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal).

When following the sample, replace any "{ACR_*}" values with the correct values for your container repository.

Be sure to log into the container respository from your device.

```
PS  D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker login {ACR_NAME}.azurecr.io {ACR_USER} {ACR_PASSWORD}
```

## Containerize the sample app

The x64 containers can be build directly on a PC.
For the remainder of this sample, we will use the environment variable $Container to refer to the address of our container.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> $Container = "{ACR_NAME}.azurecr.io/serialioports:1.0.0-x64"

PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker build . -f .\Dockerfile.windows-x64 -t $Container

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
Step 5/5 : CMD [ "SerialIOPorts.exe", "-rte", "-dCOM3" ]
 ---> Running in 1aedd449ffa4
Removing intermediate container 1aedd449ffa4
 ---> d6cbd51600e3
Successfully built d6cbd51600e3
Successfully tagged {ACR_NAME}.azurecr.io/serialioports:1.0.0-x64
```

## Test the sample app on the device

At this point, we'll want to run the container locally to ensure that it is able to find and talk to our peripheral.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker run --device "class/86E0D1E0-8089-11D0-9CE4-08003E301F73" --isolation process $Container SerialIOPorts.exe

SerialIOPorts 1.0.0.0
  -h, --help                 show this message and exit
  -l, --list                 list available devices and exit
  -d, --device=ID            the ID of device to connect
  -r, --receive              receive and display packets
  -t, --transmit             transmit packets (combine with -r for loopback)
  -c, --config               display device configuration
  -e, --edge                 transmit through azure edge
Available devices:
COM1
COM3
```

This will show that the application is able to run through the container, and list the available devices. 
Notice that we are overriding the entry point from the command line.

Now let's pick a device and ensure we can open it:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker run --device "class/86E0D1E0-8089-11D0-9CE4-08003E301F73" --isolation process $Container SerialIOPorts.exe -c -dCOM3

11/20/2018 8:11:29 AM Connecting to device COM3}...
=====================================
BaudRate: 0x1C200
Parity: 0x2
StopBits: 0x0
=====================================
```

## Push the container

Now, we push the container into the repository which we built earlier. At this point, the container image is waiting for us to deploy.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker push $Container

The push refers to repository [{ACR_NAME}.azurecr.io/serialioports]
bf4863b963b0: Preparing
3ed1316f55e1: Preparing
b4d9f6916bae: Preparing
6d34deee1fa7: Preparing
79c160bd8d82: Preparing
79c160bd8d82: Skipped foreign layer
b4d9f6916bae: Mounted from serialioports
bf4863b963b0: Pushed
6d34deee1fa7: Mounted from serialioports
3ed1316f55e1: Pushed
1.0.0-arm32: digest: sha256:9593c87a18198915118ecbdc7f5a308dbe34a15ef898bac3fb5d06730ae6d30a size: 1464
```

## Edit the deployment.json file

In the repo, you will find a sample deployment.x64.json files.
Search for "ACR_" and replace those values with the correct values for your container repository.
The ACR_IMAGE must exactly match what you pushed, e.g. jcoliz.azurecr.io/serialioports:1.0.0-x64

```
    "$edgeAgent": {
      "properties.desired": {
        "runtime": {
          "settings": {
            "registryCredentials": {
              "{ACR_NAME}": {
                "username": "{ACR_USER}",
                "password": "{ACR_PASSWORD}",
                "address": "{ACR_ADDRESS}"
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

You can now deploy this deployment.json file to your device.
For reference, please see [Deploy Azure IoT Edge modules from Visual Studio Code](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-vscode)

## Verify

Using the Azure IoT Edge extension for Visual Studio Code, you can select your device and choose "Start Monitoring D2C Message". You should see this:

```
[IoTHubMonitor] [4:04:43 PM] Message received from [{ACR_NAME}/serialioports]:
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
[IoTHubMonitor] [4:04:44 PM] Message received from [{ACR_NAME}/serialioports]:
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
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker ps

CONTAINER ID        IMAGE                                                                           COMMAND                  CREATED              STATUS              PORTS                                                                  NAMES
b4107d30a29d        {ACR_NAME}.azurecr.io/serialioports:1.0.2-x64                                   "SerialIOPorts.exe -rt…"   About a minute ago   Up About a minute                                                                     serialioports
56170371f8f5        edgeshared.azurecr.io/microsoft/azureiotedge-hub:1809_insider-windows-arm32     "dotnet Microsoft.Az…"   3 days ago           Up 6 minutes        0.0.0.0:443->443/tcp, 0.0.0.0:5671->5671/tcp, 0.0.0.0:8883->8883/tcp   edgeHub
27c147e5c760        edgeshared.azurecr.io/microsoft/azureiotedge-agent:1809_insider-windows-arm32   "dotnet Microsoft.Az…"   3 days ago           Up 7 minutes                                                                               edgeAgent
```

Then, use the ID for the serialioports container to check the logs

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialIOPorts\CS> docker logs b4107d30a29d
11/21/2018 4:48:33 PM Connecting to device COM3...
11/21/2018 4:48:34 PM Write 1 Completed. Wrote 30 bytes: "00001/049.18,053.69/077.32,024"
11/21/2018 4:48:34 PM Read 1 Completed. Received 30 bytes: "00001/049.18,053.69/077.32,024"
11/21/2018 4:48:35 PM SendEvent: [{"machine":{"temperature":49.18,"pressure":53.69},"ambient":{"temperature":77.32,"humidity":24},"timeCreated":"2018-11-21T16:48:34.5113171-08:00"}]
11/21/2018 4:48:35 PM Write 2 Completed. Wrote 30 bytes: "00002/061.68,097.79/053.30,026"
11/21/2018 4:48:35 PM Read 2 Completed. Received 30 bytes: "00002/061.68,097.79/053.30,026"
11/21/2018 4:48:35 PM SendEvent: [{"machine":{"temperature":61.68,"pressure":97.79},"ambient":{"temperature":53.3,"humidity":26},"timeCreated":"2018-11-21T16:48:35.5104208-08:00"}]
11/21/2018 4:48:36 PM Write 3 Completed. Wrote 30 bytes: "00003/069.86,014.59/018.92,025"
11/21/2018 4:48:36 PM Read 3 Completed. Received 30 bytes: "00003/069.86,014.59/018.92,025"
11/21/2018 4:48:36 PM SendEvent: [{"machine":{"temperature":69.86,"pressure":14.59},"ambient":{"temperature":18.92,"humidity":25},"timeCreated":"2018-11-21T16:48:36.5173581-08:00"}]
11/21/2018 4:48:37 PM Write 4 Completed. Wrote 30 bytes: "00004/030.22,061.69/088.52,024"
11/21/2018 4:48:37 PM Read 4 Completed. Received 30 bytes: "00004/030.22,061.69/088.52,024"
11/21/2018 4:48:37 PM SendEvent: [{"machine":{"temperature":30.22,"pressure":61.69},"ambient":{"temperature":88.52,"humidity":24},"timeCreated":"2018-11-21T16:48:37.5398929-08:00"}]
11/21/2018 4:48:38 PM Write 5 Completed. Wrote 30 bytes: "00005/090.27,075.66/053.89,025"
11/21/2018 4:48:38 PM Read 5 Completed. Received 30 bytes: "00005/090.27,075.66/053.89,025"
11/21/2018 4:48:38 PM SendEvent: [{"machine":{"temperature":90.27,"pressure":75.66},"ambient":{"temperature":53.89,"humidity":25},"timeCreated":"2018-11-21T16:48:38.5312768-08:00"}]
```