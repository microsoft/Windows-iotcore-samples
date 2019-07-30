---
name: Waveshare KMDF Filter Driver
description: Filter driver which enables the Waveshare touchscreen on IoT Core.
page_type: sample
urlFragment: virtual-pwm
languages:
  - csharp
products:
  - windows
---

# Waveshare KMDF Filter Driver

This sample filter driver enables touch events on the Waveshare 7" HDMI LCD Touchscreen for use on Windows IoT Core.  The driver is a HidUsb KMDF Lower Filter Driver.  The driver alters the HID input reports received from the touchscreen to comply with the HID specification.

This driver resolves a long-standing issue seen with this specific touchscreen where finger-up events were not recognized in Windows IoT Core.

This driver was developed and tested using the Waveshare 7" HDMI LCD Touchscreen Rev. 2.1.

## Related technologies
[Windows Driver Frameworks](http://msdn.microsoft.com/en-us/library/windows/hardware/ff557565)

## Building the driver
Before starting, make sure you have the latest version of Visual Studio, Windows Software Development Kit, and Windows Driver Kit.

To build the driver:
1. Clone the repository.
2. Open filter.vcxproj in Visual Studio.
3. Select a build config (Release or Debug) and target architecture (x86, x64, arm).
4. Build the solution.

After a successful build, the driver will be placed in the following folder: WaveshareFilterKmdf\\\<*Arch*\>\\<*Config*\>\

## Installing the driver into your IoT Core image
In order to add any driver to your IoT Core image, you must first package it into a cab file.  Instructions to do so are in the [IoT Core Manufacturing Guide](https://docs.microsoft.com/en-us/windows-hardware/manufacture/iot/add-a-driver-to-an-image). Please follow the lab to add a driver to an image.

Note: Since this driver is for an existing device, you will not need to create or add an ACPITABL.dat.

## Additional resources
* [Windows 10 IoT Core home page](https://developer.microsoft.com/en-us/windows/iot/)
* [Windows Driver Samples](https://github.com/Microsoft/Windows-driver-samples/)

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact <opencode@microsoft.com> with any additional questions or comments.
