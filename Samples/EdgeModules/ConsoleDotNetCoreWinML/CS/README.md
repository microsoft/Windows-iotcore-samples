# Windows IoT Edge WinML Sample

This is a sample showing an Azure IoT Edge deployment for Windows IoT Core with a .netcore c# console app to demonstrate Camera capture and Windows Machine Learning(WinML) to do image recognition from an Azure IoT Edge Module in a docker process isolation container(aka Argon container)

## App Overview
This sample contains a .onnx model trained with Azure Custom Vision Service( https://azure.microsoft.com/en-us/services/cognitive-services/custom-vision-service/ ) to recognize a pear, an apple, grapes, and a pen.
There are example photos of the objects in the resources directory of the project.
When the object recognized changes the new object label is reported back to Azure IoT Hub in the module properties twin and also as a device2cloud
telemetry stream message that can be capture to a storage account or sent to an event hub https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messages-d2c  

You can change the model by providing a different model file and updating the loss dictionary in model.cs with new labels.
The code assumes the model input and output features are in the form of a standard Azure Customer Vision Server *CoreML* format model converted to .onnx with the standard python onnx image conversion tools. https://github.com/onnx/onnxmltools 
If you do change models you may also want to change the property names to match the new label type in the updateobject methods in the AzureConnection class in AzureHelper.cs

The sample currently uses the CPU to do the WinML because that's available on all iot core boards.  To choose GPU evaluation you can change the LearningModelDeviceKind in the CreateModelAsync function in model.cs
However, as of this writing -- November 2018, there are no supported GPU drivers for any IoT Core capable devices that have been upgraded to a sufficiently recent WDDM driver version(>=WDDM 2.4) to work in process isolation containers.

## Other things of interest in this sample
* the deployment.json file demonstrates container creation options for exposing camera and gpu into the container.

### Prerequisites
* azure subscription with the following resources:
    * iot hub https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-create-through-portal
    * storage  https://docs.microsoft.com/en-us/azure/storage/common/storage-quickstart-create-account
    * container registry  https://docs.microsoft.com/en-us/azure/container-registry/container-registry-get-started-portal
* hardware:
    * board with IoT Core installed
    * USB web cam
* required packages to install
    * windows sdk for 1809 https://developer.microsoft.com/en-us/windows/downloads/sdk-archive
    * .netcore 2.2 (TODO: provide url)    
    * azure device client for iot edge https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-sdks
    * either visual studio, vscode, or .net core dotnet.exe build environment

### Build and Publish the app
#### Visual Studio
1. load the solution
2. right click on the cs project and select publish
    __*note*__ Visual Studio won't allow you to select win-arm from the publish configuration dialog. but, if you edit the properties\FolderProfile.pubxml file directly with VS or another editor and set RuntimeIdentifier to win-arm the publish button will
    do the right thing after that.
#### Dotnet CLI
1. cd to the directory container the .csproj
2. dotnet publish
#### VsCode
1. TODO: figure out what to do for this case or remove this section


### build module container for the app

#### container build for arm32:
* pull dotnet iotuap container
* copy the app and docker file to the device.  there is a template docker file in ConsoleDotNetCoreWinML\cs\ConsoleDotNetCoreWinML\Properties\Dockerfile.IoTUAP
* docker build -f <path to dockerfile> -t <your_container_repo/iotuap:17763.arm32.ConsoleDotNetCoreWinML-l>

#### container build for amd64:
* this is the same as arm32. It can't be done from dev desktop since the container support for target guest is not present due to the win32kmin/full issue.

### update deployment.nocreds.json
the "preview" creds in the deployment.json are public read-only creds to the standard edge runtime modules.
But, for the sample you must update the creds in the deployment.json file to provide access to your module container repository where you've stored the module you just built.

TODO: point to various ways to do this.
simplest imho is VSCode with Azure IoT Edge extension.






