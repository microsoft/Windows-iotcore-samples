# Azure IoT Edge on Windows: USB Serial Access (Serial.IO.Ports)

This sample demonstrates how to create a module for Azure IoT Edge on Windows 10 IoT devices to communicate with peripherals connected via the serial bus.

In this case, we will extend the idea from the [simulated temperature sample](https://docs.microsoft.com/en-us/azure/iot-edge/quickstart). The sample will create simulated temperature data, transmit it via serial, then receive it via serial loopback and send the results up to Azure IoT Hub. 

## API Surface

This sample uses Win32 platform APIs to access the serial devices. 
Unfortunately, the standard .NET APIs for serial devices (System.IO.Ports) only work with serial devices which come named "COM{x}".
On Windows IoT Core, serial devices are not so-named.
Using the win32 platform APIs ensures maximum compatibility with all Windows IoT host platforms, and maximum speed.

## Prerequisites

### Target Hardware

* A PC running [Windows 10 - Build 17763](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewiso) to run the solution 
* Or a [Minnowboard Turbot](https://minnowboard.org/minnowboard-turbot/) running [Windows 10 IoT Core - Build 17763](https://developer.microsoft.com/en-us/windows/iot). Currently, the sample runs only on x64 architecture. Future releases will include support for arm32 architecture.
* A USB FTDI cable. I recommend the [FTDI Serial TTL-232 cable](https://www.adafruit.com/product/70). Connect the RX and TX lines with a single wire. Plug this into your device.
* [Azure IoT Edge for Windows - 1.0.6 or higher](https://docs.microsoft.com/en-us/azure/iot-edge/) 

### Azure Subscription

* [Azure IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal). This is your Cloud gateway which is needed to manage your IoT Edge devices. All deployments to Edge devices are made through an IoT Hub. You can use the free sku for this sample.
* [Azure Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal). This is where you host your containers (e.g. IoT Edge modules). Deployment manifests refer to this container registry for the IoT Edge devices to download their images. You can use the free sku for this sample.

### Development Machine

* [Visual Studio Code with Azure IoT Edge extension](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-vscode). The IoT Edge development environment, including the extension that connects to your IoT Hub and lets you manage your IoT Devices and IoT Edge Devices right from VS Code.
* [Windows 10 - Build 17763 or higher](https://www.microsoft.com/en-us/software-download/windowsinsiderpreviewiso) to build the solution. This can be the same machine you're running the solution on, or a different one.

### Working with Docker

Azure IoT Edge installs a custom build of the moby (aka Docker) container engine. In order to use the docker command line as described in this sample, you'll have some additional setup to do.

* Download a recent docker command line tool from dockerproject.org. Put this somewhere on your path. It's available at [https://master.dockerproject.org/windows/x86_64/docker.exe](https://master.dockerproject.org/windows/x86_64/docker.exe). This is required because the command line tool distributed with Azure IoT Edge does not yet include the '--device' option, as of the time of this writing. 
* Set the DOCKER_HOST environment variable to "npipe:////./pipe/iotedge_moby_engine". This will ensure the docker command line tool is communicating with the correct docker engine.

In order to verify your configuration, run the 'docker version' command. Then compare with the below:

```
PS C:\Windows-iotcore-samples\Samples\EdgeModules\SqueezeNetObjectDetection\cs> docker version
Client:
 Version:           master-dockerproject-2019-02-12
 API version:       1.40
 Go version:        go1.11.5
 Git commit:        7f612bfc
 Built:             Tue Feb 12 23:42:34 2019
 OS/Arch:           windows/amd64
 Experimental:      false

Server:
 Engine:
  Version:          3.0.3
  API version:      1.40 (minimum version 1.24)
  Go version:       go1.11.4
  Git commit:       5ec3138
  Built:            Thu Jan 24 17:16:18 2019
  OS/Arch:          windows/amd64
  Experimental:     false
```

The server should be version 3.0.3 or higher, built Jan 24 2019 or later. The client should be built Feb 12 2019 or later.

## Build the sample

1. If you download the samples ZIP, be sure to unzip the entire archive, not just the folder with the sample you want to build.
2. Open a PowerShell window.
3. Change directory to the folder where you unzipped the samples, go to the **Samples/EdgeModules** subfolder, then the subfolder for this sample (**SerialWin32/CS**).
3. Build and publish the sample using dotnet command line:

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> dotnet publish -r win-x64
Microsoft (R) Build Engine version 15.8.166+gd4e8d81a88 for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.

  Restore completed in 53.44 ms for D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\SerialWin32.csproj.
  SerialWin32 -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\bin\Debug\netcoreapp2.1\win-x64\SerialWin32.dll
  SerialWin32 -> D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS\bin\Debug\netcoreapp2.1\win-x64\publish\
```

## Containerize the sample app

When following this document, replace any "{ACR_*}" values with the correct values for your [container registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal).

Log into your container registry from the device where you will be building the containers. If you are using the azure command line tools, you can use "az acr login" as described in the article linked above. Alternately, you can do it directly with the docker command line:

```
PS C:\data\modules\i2ctemp> docker login {ACR_NAME}.azurecr.io -u {ACR_USER} -p {ACR_PASSWORD}
```

The x64 containers can be build directly on your development PC.
For the remainder of this document, we will use the environment variable $Container to refer to the address of our container.

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> $Container = "{ACR_NAME}.azurecr.io/serialwin32:1.0.0-x64"

PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> docker build bin\Debug\netcoreapp2.1\win-x64\publish\ -t $Container

Sending build context to Docker daemon  81.89MB
Step 1/5 : mcr.microsoft.com/windows/nanoserver:1809
 ---> 91da8a971b53
Step 2/5 : ARG EXE_DIR=.
 ---> Running in b537bd4962d6
Removing intermediate container b537bd4962d6
 ---> 6d6281589c30
Step 3/5 : WORKDIR /app
 ---> Running in b8f3943ab2e5
Removing intermediate container b8f3943ab2e5
 ---> 37f5488097e5
Step 4/5 : COPY $EXE_DIR/ ./
 ---> 49f265682955
Step 5/5 : CMD [ "SerialWin32.exe", "-rs", "-dPID_6001" ]
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

Now, we push the container into the registry. Afterward, the container image is waiting for us to deploy.

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
1.0.0-x64: digest: sha256:9593c87a18198915118ecbdc7f5a308dbe34a15ef898bac3fb5d06730ae6d30a size: 1464
```

## Edit the deployment.json file

In the repo, you will find separate deployment.{arch}.json files for each architecture.
Choose the deployment file corresponding to your deployment atchitecture, then fill in the details for your container image.
Search for "{ACR_*}" and replace those values with the correct values for your container registry.
The ACR_IMAGE must exactly match what you pushed, e.g. jcoliz.azurecr.io/serial-module:1.0.0-x64

```
    "$edgeAgent": {
      "properties.desired": {
        "runtime": {
          "settings": {
            "registryCredentials": {
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

## Deploy edge modules to device

Back on your PC, you can now deploy this deployment.json file to your device.
For reference, please see [Deploy Azure IoT Edge modules from Visual Studio Code](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-vscode)

## Verify device messages

Using the Azure IoT Edge extension for Visual Studio Code, you can select your device and choose "Start Monitoring D2C Message". You should see this:

```
[IoTHubMonitor] [4:04:43 PM] Message received from [device/serialwin32]:
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
[IoTHubMonitor] [4:04:44 PM] Message received from [device/serialwin32]:
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

First, list the modules

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> iotedge list
NAME             STATUS           DESCRIPTION      CONFIG
serialwin32      running          Up 5 minutes     {ACR_NAME}.azurecr.io/serialwin32:1.0.0-x64
edgeHub          running          Up 33 minutes    mcr.microsoft.com/azureiotedge-hub:1.0.6
edgeAgent        running          Up 33 minutes    mcr.microsoft.com/azureiotedge-agent:1.0.6
```

Then, check the logs

```
PS D:\Windows-iotcore-samples\Samples\EdgeModules\SerialWin32\CS> PS C:\data\modules\i2ctemp> iotedge logs --tail 5 serialwin32
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
