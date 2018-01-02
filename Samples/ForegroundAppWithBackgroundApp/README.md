---
title: Foreground App with Background App sample
ms.author: brian.fjeldstad
description: An example of building a foreground app and background app within the same APPX file.
---

# “Foreground App with Background App”

These are the available versions of this Windows 10 IoT Core sample.  

In both versions, the Background App currently toggles a GPIO pin.  If you are using a Dragonboard, 
you'll need to change LED_PIN in StartupTask.cpp (for C++) or StartupTask.cs (for C#) to a pin that 
exists on the Dragonboard (for example, the User LED 1: pin 21).  You can find a list of available
pins for the Dragonboard [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/pinmappings/pinmappingsdb).

*	[C#](./CS/README.md)
*	[C++](./CPP/README.md)

## Additional resources
* [Windows 10 IoT Core home page](https://developer.microsoft.com/en-us/windows/iot/)

This project has adopted the Microsoft Open Source Code of Conduct. For more information see the Code of Conduct FAQ or contact <opencode@microsoft.com> with any additional questions or comments.
