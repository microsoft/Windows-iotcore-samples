---
title: Virtual PWM Driver Sample
ms.author: mlotfy
description: This is a Windows Universal Driver sample for PWM that implements the PWM DDI.
---

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

For best experience using the PWM WinRT APIs from UWP apps, some device properties may need to be set where some of which are generally required for UWP access to the PWM device interface while others are required for some PWM WinRT APIs to function correctly.

### DEVPKEY_DeviceInterface_Restricted

Per the current UWP device access model, setting Restricted device interface property to FALSE is required to give UWP apps access to the PWM device interface. For more on that device interface property see MSDN documentation.

### DEVPKEY_DeviceInterface_SchematicName

Assigning a schematic name to the PWM device interface of statically connected PWM devices is required for using the `PwmController.GetDeviceSelector(FriendlyName)` factory method. The schematic name is the name given to the PWM device in the system design schematics e.g (PWM0, PWM_1, et..). Schematic names are assumed to be unique across the system, but that is not enforced by the OS. At least, there shouldn’t be 2 PWM devices having the same schematic name, otherwise the WinRT PWM `PwmController.GetDeviceSelector(FriendlyName)` behavior will be non-deterministic.

A UWP C# example for PMW device enumeration using `PwmController.GetDeviceSelector` with the FriendlyName overload:

```csharp
    string selector = PwmController.GetDeviceSelector("VPWM0");
    DeviceInformationCollection deviceCollection =
                await DeviceInformation.FindAllAsync(selector);
```

### Common methods for setting DeviceInterface properties

#### Method 1: Using the INF file

The `AddProperty` directive can be used to set device properties. The INF file should be written in a way that allows setting different values for the same property on one or subset of the PWM device instances.

Taking the Restricted property as an example, not all designs will necessarily have the same policy of which PWM device to expose to UWP, it can be that a certain design wants to allow UWP access only to a subset of the PWM device instances. Assume an SoC based design where there are 4 identical PWM device instances (IP Blocks) named PWM0,…,PWM3 where their ACPI assigned Hardware ID (_HID) is FSCL00E0 and their Unique ID (_UID) is 0,…,3. Exposing all the PWM devices to UWP will require the INF sections that sets the Restricted property to match on the Hardware ID ACPI\FSCL00E0.

Example setting `DEVPKEY_DeviceInterface_Restricted`:

```
;*****************************************
; Device interface installation
;*****************************************
[PWM_Device.NT.Interfaces]
AddInterface={60824B4C-EED1-4C9C-B49C-1B961461A819},,PWM_Interface

[PWM_Interface]
AddProperty=PWM_Interface_AddProperty

; Set DEVPKEY_DeviceInterface_Restricted property to false to allow UWP access
; to the device interface without the need to be bound with device metadata.
; If Restricted property is set to true, then only applications which are bound
; with device metadata would be allowed access to the device interface.
[PWM_Interface_AddProperty]
{026e516e-b814-414b-83cd-856d6fef4822},6,0x11,,0
```

Exposing a subset of the PWM devices - or generally assigning a different property value to one or a subset of the PWM device instances - will require having different Hardware ID for each PWM device instance and match INF sections selectively based on the policy.

#### Method 2: Programmatically from within the driver

Drivers can use `IoSetDeviceInterfacePropertyData` to set device interface properties in `EVT_WDF_DRIVER_DEVICE_ADD` after it creates and publishes the PWM device interface. Deciding which value to assign to which device property in the driver is an implementation detail, but ACPI is usually the natural place to store such information for SoC based designs.
For ACPI and SoC based PWM devices, the value of each device interface property can be specified in each ACPI device node _DSD method as Device Properties. The driver will need to query the _DSD from ACPI, parse the Device Properties data, extract the value of each property and assign it device interface.

Example setting `DEVPKEY_DeviceInterface_Restricted`:

```
DEVPROP_BOOLEAN isRestricted = DEVPROP_FALSE;
status =
    IoSetDeviceInterfacePropertyData(
        &symlinkNameWsz,
        &DEVPKEY_DeviceInterface_Restricted,
        0,
        0, // Flags
        DEVPROP_TYPE_BOOLEAN,
        sizeof(isRestricted),
        &isRestricted);
```

#### Method 1 VS Method 2
*The INF method:*
- Pros: Is much easier to implement and requires no driver code changes, and servicing an INF file is easier than that for a driver binary
- Cons: Requires a tailored INF file to each design.

*The ACPI based method:*
- Pros: Makes the driver and its INF file portable across designs and hence BSPs where the only change would be in the ACPI DSDT defining each PWM device node.
- Cons: Reading and parsing ACPI binary blocks is a tedious work that requires a lot of code which will be prone to errors and vulnerabilities resulting in a bigger error surface.
Between the 2 methods, the INF method should be the way to go whenever applicable.

It is up to the driver author to choose between the 2 methods as long as the trade-offs are well understood and taken into considerations.

## References
- [PWM DDI](https://docs.microsoft.com/en-us/windows/desktop/devio/pwm-api).
- [PWM WinRT APIs](https://docs.microsoft.com/en-us/uwp/api/windows.devices.pwm).
- [PWM driver for i.MX platforms](https://github.com/ms-iot/imx-iotcore/tree/public_preview/driver/pwm/imxpwm).

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact <opencode@microsoft.com> with any additional questions or comments.