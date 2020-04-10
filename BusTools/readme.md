---
page_type: sample
urlFragment: bustools
languages:
  - cpp
products:
  - windows
description: "IoT Bus tools to interact with Gpio, I2c, Pwm, Spi and UART."
---

# Windows IoT Bus Tools

This folder contains tools that let you interact with Gpio, I2c, Pwm, Spi, and UART on the command line. They will run on any edition of Windows, including Windows IoT Core and Windows Enterprise. The tools are:

- GpioTestTool
- I2cTestTool
- PwmTestTool
- SpiTestTool
- MinComm (UART)

## Building

1. Download [Visual Studio 2017](https://www.visualstudio.com/downloads/). Select options for C++, Windows UWP, and Windows Desktop app development. You may need to download the latest version of the [Windows SDK](https://developer.microsoft.com/en-us/windows/downloads/windows-10-sdk).
1. From the start menu, run `Developer Command Prompt for VS 2017`
1. Navigate to the root of this repository and run:

```powershell
msbuild /p:Platform=ARM /p:Configuration=Release
```

Valid values of Platform are: `ARM, ARM64, x86, x64`

Valid values of Configuration are: `Release, Debug`