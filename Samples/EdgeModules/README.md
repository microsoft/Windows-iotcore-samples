---
page_type: sample
urlFragment: edge-modules
languages:
 - csharp
products:
 - windows
description: Learn how to leverage sample modules to demonstrate the various features of Azure IoT Edge on Windows.
---

# Azure IoT Edge Modules on Windows IoT

This directory contains sample modules demonstrating various features of Azure IoT Edge on Windows.
Each of these samples can be treated as standalone samples that show how to use each Windows feature from an Azure IoT Edge module.
Additionally, several of them are designed to work together to demonstrate coordinating a set of modules in cooperation and distributed across multiple IoT Edge devices.  See [MultiModule Architecture](./readme.multimodule.md) for an overview of how the multiple module coordination is designed and installed.

## Samples

| Readme | C# Project |
|--------| ---------- |
|[Window Machine Learning (WinML) with Azure Custom Vision Service Fruit Object Classifier Model](./WinMLCustomVisionFruit/Readme.md)| [FruitWinML](./WinMLCustomVisionFruit/CS/WinMLCustomVisionFruit.csproj)|
|[Serial I/O using PInvoke to Config Manager and Classic Win32 Serial APIs)](./SerialWin32/Readme.md)| [SerialWin32](./SerialWin32/CS/SerialWin32.csproj)|
|[Windows Machine Learning using SqueezeNet Object Detection Model](./squeezenetobjectdetection/Readme.md) from the [ONNX Model Zoo](https://github.com/onnx/models/tree/master/squeezenet)| [SqueezeNetObjectDetection](./squeezenetobjectdetection/cs/squeezenetobjectdetection.csproj)|
|[GPIO using standard WinRT Simple Peripheral Bus APIs](./gpio/Readme.md)| [GPIO](./Gpio/CS/GPIOFruit.csproj)|
|[Azure Function that responds to Azure IoT Hub events and reflects GPIO state across devices](./HubEventHandler/README.Md)| [HubEventHandler](./HubEventHandler/cs/HubEventHandler.csproj)|
|[I2C TempHumidity Sensor](./I2CTempHumidity/README.md)|[I2CTempHumidity](./I2CTempHumidity/CS/I2CTempHumidity.csproj)|
|[I2C Accelerometer](./I2C_mpu6050/README.md)|[I2C_mpu6050](./I2C_mpu6050/CS/I2C_mpu6050.csproj)|
|[SPI Accelerometer](./SPI_mpu9050/README.md)|[SPI_mpu9050](./SPI_mpu9050/CS/SPI_mpu9050.csproj)|
|[Pulse Width Modulated Fan Motor](./PWMFruit/README.md)|[PWMFruit](./PWMFruit/CS/PWMFruit.csproj)|
|[Serial IO using legacy comX ports with classic .net APIs](./SerialIoPorts/README.md)|[SerialIoPorts](./SerialIoPorts/CS/SerialIoPorts.csproj)|
|[Serial IO using WinRT serial device API](./UART_lcd/README.md)|[UART_lcd](./UART_lcd/CS/UART_lcd.csproj)|
|[Common library for shared utilities and abstract base classes](./Common/README.Md)| [Common](./common/cs/common.csproj)|

### Building the samples

All the projects reference a common MSBuild properties file in the root of the project set called [Common.CS.props](./Common.CS.props) .  In addition to other things, this file contains defaults for the windows sdk root path and the windows sdk version.  If you have different values you can either modify your copy of the props file, or override them from environment variables with the same names as the prop variables -- WindowsSdkRoot and WindowsSdkVersion

### Running the samples

These samples can be run on Window IoT Core or Windows IoT Enterprise.  If you use Enterprise then you will need to be sure the optional Windows container feature is enabled.  If you use IoT Core then you need a .ffu image that contains the Windows Container Optional Feature.
