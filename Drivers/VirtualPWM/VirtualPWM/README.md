# Virtual PWM Driver Sample

This is a Windows Universal Driver sample for PWM that implements the PWM DDI as documented on [MSDN here](https://docs.microsoft.com/en-us/windows/desktop/devio/pwm-api). A PWM DDI compliant PWM driver will allow user-mode UWP apps to access the PWM controllers through the PWM WinRT APIs as documented on [MSDN here](https://docs.microsoft.com/en-us/uwp/api/windows.devices.pwm).

## Features

with the following features:
- Exposes 2 root enumerated controller instances (With IDs ROOT\VPWM000A and ROOT\VPWM000B).
- Each controller exposes 4 virtual PWM channels.
- Supports frequencies from 4Hz to 16KHz.
- Assigns `VPWM0` and `VPWM1` as the a friendly/schematic name to the first and the second controller interfaces in the INF file. Making it easy to explicitly select each one individually using the Windows.Devices.Enumeration APIs `DeviceInformation.FindAllAsync(aqsFilter)`.
- Marks `VPWM0` in the INF file as non-restricted allowing it to be accessible from any UWP app. While `VPWM1` access is left in its default state which is being accessible/restricted only to UWP apps bound to `VPWM1` interface with metadata.
- Supports WPP and IFR tracing with the trace GUID `{E2BDF62D-48DA-4195-B31C-F47D1AB8015C}`.

## Installing the driver into your IoT Core image
In order to add any driver to your IoT Core image, you must first package it into a cab file.  Instructions to do so are in the [IoT Core Manufacturing Guide](https://docs.microsoft.com/en-us/windows-hardware/manufacture/iot/add-a-driver-to-an-image). Please follow the lab to add a driver to an image.

## Porting
The following are the main files to modify when porting the driver to a specific hardware PWM peripheral:
- `virtualpwm.inf`: Tailor the INF file per needs (e.g assign proper HID, assign the desired controllers interface friendly/schematic names, mark non-restricted interfaces, etc..).
- `virtualpwm.hpp`: Define the number of PWM channels and frequency/period valid range.
- `controller.cpp`: The PWM controller hardware abstraction module, where all the PWM hardware specific implementation is hosted.

## Setting Device Properties

For best experience using the PWM WinRT APIs from UWP apps, some device properties may need to be set where some of which are generally required for UWP access to the PWM device interface while others are required for some PWM WinRT APIs to function correctly. For more details, see [Setting device interface properties section](https://docs.microsoft.com/en-us/windows-hardware/drivers/spb/pulse-width-controller%20driver#setting-device-interface-properties) of the PWM DDI documentation.

## References

- [PWM DDI](https://docs.microsoft.com/en-us/windows-hardware/drivers/spb/pulse-width-controller%20driver).
- [PWM Driver Reference](https://docs.microsoft.com/en-us/windows/desktop/devio/pwm-api).
- [PWM WinRT APIs](https://docs.microsoft.com/en-us/uwp/api/windows.devices.pwm).
- [PWM driver for i.MX platforms](https://github.com/ms-iot/imx-iotcore/tree/public_preview/driver/pwm/imxpwm).

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact <opencode@microsoft.com> with any additional questions or comments.
