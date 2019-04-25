# Windows IoT Edge GPIO Sample

This is a sample showing an Azure IoT Edge deployment for Windows IoT Core with a .NET Core C# console app to demonstrate GPIO control of LEDs.  Additionally, it can do this in a way that can coordinate with the Fruit WinML sample.

## App Overview

The GPIO sample expects to be run on a board that is connected to 4 LEDS -- red, yellow, green, and blue.  The pins are configured via the [Module Twin's Desired Properties](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-module-twins).  the project contains example deployments for [Minnowboard Max](https://minnowboard.org/), and [Hummingboard](https://www.solid-run.com/product/SRMX6QDW00D02GE008E00CH) (TODO: take photos of the pinout and add to here).
When the sample receives a device to cloud telemetry message of the type 'FruitMessage' from another module in the system, or a SetFruit direct method call from the azure function , it extracts the recognized object and maps it to one of the colors.  then it sets that color LED to on and turns any previously set led off.
if it receives an OrientationChanged message it inverts the sense of the LEDS and turns off the LED corresponding to the current fruit and turns all the others on.
The FruitMessage can either come from a local instance of the Fruit WinML sample or from the cloud via reflection using the EventHubHandler Function sample.
The EventHubHandler can mirror the 'FruitMessage' to multiple GPIO module instances running on multiple boards.  This simulates a remote status display in a separate location.

## Other things of interest in this sample

* The deployment.json file demonstrates container creation options for exposing GPIO controllers into the container.

### Prerequisites

* Azure subscription with the following resources:
    * [IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal)
    * [Storage](https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account)
    * [Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal)
    * [Function App]() with the [EventHubHandler Example] (..\..\EventHubHandler\Readme.md) installed if you wish to exercise the device state mirroring.
* Hardware:
    * An x64 Board with an 1809 version of IoT Core installed.
    * USB web cam
* Required packages to install
    * [Windows sdk for 1809](https://developer.microsoft.com/en-us/windows/downloads/sdk-archive)
    * [.NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2)
    * [Azure device client for iot edge](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks)
    * Either Visual Studio, VSCode, or the .NET Core dotnet.exe build environment

### Build and Publish the app

#### Dotnet CLI

1. cd to the directory container the .csproj
2. dotnet publish

#### VSCode

1. See [Edge development How-To](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-develop-csharp-module)

#### Visual Studio

1. Load the solution
2. Right click on the cs project and select publish.  
    __*note:*__ Visual Studio won't allow you to select win-arm from the publish configuration dialog. But, if you edit the properties\FolderProfile.pubxml file directly with VS or another editor and set RuntimeIdentifier to win-arm the publish button will do the right thing after that.

### Build module container for the app

#### Container build for amd64

Unfortunately, this can't be done from a developer desktop since the iot core container cannot run on desktop.  Instead it must be done on real hardware.  After you have published your app do the following steps:

* Obtain an x64 machine with IoT Core installed.
* ssh into a command prompt on the machine.
* Install Docker on the IoT Core machine
    * Download x64 [docker.exe](https://master.mobyproject.org/windows/x86_64/docker.exe) 
    * And x64 [dockerd.exe](https://master.mobyproject.org/windows/x86_64/dockerd.exe)
    * Copy them to the IoT Core machine and put docker.exe in the path.
    * from a command prompt run 'dockerd.exe --register-service'
    * sc.exe start docker
* Copy your app to the machine
* (TODO: Edit the dockerfile in properties\Dockerfile.IoTUAP to point to real repo with Windows IoT Core + .NET Core)
* Copy properties\Dockerfile.IoTUAP to the machine
* Do 'docker login' with creds to your container registry.
* Do 'docker build -f < path to dockerfile.iotuap > -t <registry/repo:tag> .' to build the image.
* docker push  <registry/repo:tag>

### Update deployment.nocreds.json

The "preview" creds in the deployment.json are public read-only creds to the standard edge runtime modules.  But, for the sample you must update the creds in the deployment.json file to provide access to your module container repository where you've stored the module you just built. And, you must update the image url in the module section with the correct url for your module image.

### Deploy the module

There are multiple ways to do this:

* [Azure web portal](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal)
* [Azure Command Line CLI](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-cli)
* [VSCode plugin](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-VSCode)

The recommended way to get started is VSCode with the Azure IoT Edge extension. But, for larger scale production deployments scripting with CLI is likely preferable.
