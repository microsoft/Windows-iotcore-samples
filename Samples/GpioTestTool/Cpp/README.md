GPIO Test Tool
===============

This sample is a simple GPIO pin testing tool.  The tool is useful for verifying basic GPIO pin configuration capabilities on your Windows 10 IoT Core device.

## Set Up Build Environment

You will want to have the latest version of [Visual Studio](https://www.visualstudio.com/downloads/) along with the latest Windows 10 SDK.  Typically the Visual Studio Installer will have an option to install the latest Windows 10 SDK, but you can also download the SDK standalone [here](https://developer.microsoft.com/en-US/windows/downloads/windows-10-sdk).

## Get the Code

You can get the source code for this sample by cloning or downloading a zip of all our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/tree/master).  Navigate to ```Samples\GpioTestTool```, select the sample code version, and open the solution in Visual Studio.

## Build the Sample

In the Visual Studio project, select the target architecture (x86, x64, ARM) and version (Debug, Release) and select ```Build -> Build Solution```.

Note: You may get an SDK mismatch error.  The sample is preconfigured for Windows SDK version 10.0.17134.0, which may not be the version you have installed.  To resolve this error, right click on your solution in the Solution Explorer window and select ```Retarget Solution```.  Select your SDK version and click OK.  This will retarget the sample to use the Windows 10 SDK version you have installed.

## Run the Sample
SSH into the target device and copy the newly built GpioTestTool.exe executable to the target device.  Run GpioTestTool.exe from the SSH command prompt to see the top level options.

```
GpioTestTool: Command line GPIO testing utility
Usage: GpioTestTool.exe [-list] PinNumber

-list         List the available pins on the default GPIO controller.
PinNumber     The pin number with which you wish to interact. This
                parameter is required.

Example:
GpioTestTool.exe -list
GpioTestTool.exe 47
```

You can select a pin to interact with.  When you do, you can type ```help``` to list the pin configuration options.

```
E:\>GpioTestTool.exe 5
Type 'help' for a list of commands
> help
Commands:
 > write 0|1                        Write pin low (0) or high (1)
 > high                             Alias for 'write 1'
 > low                              Alias for 'write 0'
 > toggle                           Toggle the pin from its current state
 > read                             Read pin
 > setdrivemode drive_mode          Set the pins's drive mode
     where drive_mode = input|output|
                        inputPullUp|inputPullDown
 > interrupt on|off                 Register or unregister for pin value
                                    change events.
 > info                             Dump information about the pin
 > help                             Display this help message
 > quit                             Quit
```

For a simple demonstration:
1. Follow the ["Hello, blinky!" sample](https://github.com/Microsoft/Windows-iotcore-samples/blob/develop/Samples/HelloBlinky/Cpp/README.md) to wire an LED to your Windows 10 IoT Core device.
1. Run GpioTestTool.exe and select the GPIO pin you have wired the LED to.
1. Set the drive mode to output: ```setdrivemode output```
1. Toggle the LED using the ```toggle``` command.  You should see the LED turning on or off everytime you run ```toggle```.

## Additional resources
* [Windows 10 IoT Core home page](https://developer.microsoft.com/en-us/windows/iot/)

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact opencode@microsoft.com with any additional questions or comments.
