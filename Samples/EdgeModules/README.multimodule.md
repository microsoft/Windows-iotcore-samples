# MultiModule Overview

## Design

The fruit WinML project is trained to recognize four objects.  When it detects an object change it sends an [Azure IoT Device to Cloud Message](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messaging
) with the name of the object in the message.
The GPIO module listens for copies of those messages and maps the object name to a colored LED which is turned on and off via GPIO pins according to the following table:

|Object|LED Color|
|------|---------|
| Apple | Red |
| Grapes | Blue |
| Pear | Yellow |
| Pen | Green |

The pin number to color mappings are configurable from the GPIO [Module Twin Desired Properties](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-module-twins).  Thus, the same module application can be used for different boards with different pin header layouts.

## Installation

In you Azure Subscription you need a resource group that contains the following resources:

* Azure IoT Hub
* Azure Container Registry
* Azure Storage
* Azure Function App
