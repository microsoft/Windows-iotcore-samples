# Windows IoT Edge WinML Sample

This is a sample showing an Azure IoT Edge deployment for Windows IoT Core with a .NET Core C# console app to demonstrate Camera capture and Windows Machine Learning(WinML) to do image recognition from an Azure IoT Edge Module in a docker process isolation container(aka Argon container)

## App Overview

This sample contains a .onnx model trained with [Azure Custom Vision Service]( https://azure.microsoft.com/en-us/services/cognitive-services/custom-vision-service/ ) to recognize a pear, an apple, grapes, and a pen.
There are example photos of the objects in the resources directory of the project.
When the object recognized changes the new object label is reported back to Azure IoT Hub in the module properties twin and also as a [device2cloud](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c)
telemetry stream message that can be capture to a storage account or sent to an event hub 

You can change the model by providing a different model file and updating the loss dictionary in model.cs with new labels.
The code assumes the model input and output features are in the form of a standard Azure Customer Vision Server *CoreML* format model converted to .onnx with the [standard Python ONNX image conversion tools](https://github.com/onnx/onnxmltools).  
If you do change models you may also want to change the property names to match the new label type in the updateobject methods in the AzureConnection class in AzureHelper.cs

The sample currently uses the CPU to do the WinML because that's available on all IoT Core boards.  To choose GPU evaluation you can pass a -gpu command line switch in the Dockerfile to change the LearningModelDeviceKind in the CreateModelAsync function in model.cs
However, as of this writing -- (spring 2019), there are no supported GPU drivers for any IoT Core capable devices that have been upgraded to a sufficiently recent WDDM driver version(>=WDDM 2.4) to work in process isolation containers and are also provided in a universal driver package which is necessary to add a driver to an IoT Core image.  We expect this to change soon and plan to update this readme when something is available.
For IoT Enterprise any of the normal GPUs supported by general WinML are expected to work if they've been upgraded to the latest display drivers >= WDDM v. 2.5.

## Other things of interest in this sample

* The deployment.json file demonstrates container creation options for exposing camera and gpu into the container.

### Prerequisites

* Azure subscription with the following resources:
    * [IoT Hub](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal)
    * [Storage](https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account)
    * [Container Registry](https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal)
* Hardware:
    * An x64 Board with Windows 10 IoT Core version 1809 (Build 17763) installed.
    * USB web cam
* Required packages to install
    * [Windows 10 SDK, version 1809](https://developer.microsoft.com/en-us/windows/downloads/sdk-archive)
    * [.NET Core 2.2](https://dotnet.microsoft.com/download/dotnet-core/2.2)
    * [Azure IoT Hub Device SDK](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks)
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

### Build module container for the app

#### Container build for amd64

Unfortunately, this can't be done from a developer desktop since the IoT Core container cannot run on desktop.  Instead it must be done on real hardware.  After you have published your app do the following steps:

* Obtain an x64 machine with IoT Core installed.
* ssh into a command prompt on the machine.
* Install Docker on the IoT Core machine
    * Download x64 [docker.exe](https://master.mobyproject.org/windows/x86_64/docker.exe) 
    * And x64 [dockerd.exe](https://master.mobyproject.org/windows/x86_64/dockerd.exe)
    * Copy them to the IoT Core machine and put docker.exe in the path.
    * from a command prompt run 'dockerd.exe --register-service'
    * sc.exe start docker
* Copy your app to the machine
* Choose properties\dockerfile.dotnet or dockerfile.standalone depending on which kind of project you've built and copy that docker file to the machine
* Do 'docker login' with creds to your container registry.
* Do 'docker build -f < path to dockerfile> -t <registry/repo:tag> .' to build the image.
* docker push  <registry/repo:tag>

### Update deployment.nocreds.json

The "preview" creds in the deployment.json are public read-only creds to the standard edge runtime modules.  But, for the sample you must update the creds in the deployment.json file to provide access to your module container repository where you've stored the module you just built. And, you must update the image url in the module section with the correct url for your module image.

### Deploy the module

There are multiple ways to do this:

* [Azure web portal](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-portal)
* [Azure Command Line CLI](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-cli)
* [VSCode plugin](https://docs.microsoft.com/en-us/azure/iot-edge/how-to-deploy-modules-VSCode)

The recommended way to get started is VSCode with the Azure IoT Edge extension. But, for larger scale production deployments scripting with CLI is likely preferable.
