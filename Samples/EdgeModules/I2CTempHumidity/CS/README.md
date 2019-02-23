# I2C Access to sensor device in Azure IoT Edge on Windows

This sample will demonstrate how to create a module for Azure IoT Edge on Windows 10 IoT Core which accesses a temperature and humidity sensor connected via the I2C bus.

## Install Azure IoT Edge

These instructions work with the 1.0.6 release of [Azure IoT Edge for Windows](https://docs.microsoft.com/en-us/azure/iot-edge/), or higher.

## Host Hardware & OS

* A [Minnowboard Turbot](https://minnowboard.org/minnowboard-turbot/) running [Windows 10 IoT Core - Build 17763](https://developer.microsoft.com/en-us/windows/iot). Currently, the sample runs only on x64 architecture. Future releases will include support for arm32 architecture.

## Peripheral Hardware

* Obtain an [Si7021 Temperature & Humidity Sensor Breakout Board](https://www.adafruit.com/product/3251). 
* Connect this to your IoT core device. For example, on Minnowboard Turbot, you would connect it to the [Low Speed Expansion Header](https://minnowboard.org/tutorials/connecting-device-sensor-low-speed-expansion-lse-header).
* Please refer to the [Data Sheet](https://www.silabs.com/documents/public/data-sheets/Si7021-A20.pdf) for technical details on this device.

## Access Windows APIs

To get access to Windows.Devices.I2c namespace and various other Windows classes an assembly reference needs to be added for Windows.winmd
For this project the assembly reference is parametrized by the environment variable WINDOWS_WINMD, so you need to set this environment variable before building.
The file path for the Windows.winmd file may be: ```C:\Program Files (x86)\Windows Kits\10\UnionMetadata\[version]\Windows.winmd```
