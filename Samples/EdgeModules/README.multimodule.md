# MultiModule Overview

## Design

The fruit WinML project is trained to recognize the four objects described in the table below.  When the WinMLCustomFruit module with the USB camera and custom vision ONNX model detects an object change it sends an [Azure IoT Device to Cloud Message](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-messaging
) with the name of the object in the message.
The GPIO module listens for copies of those messages and maps the object name to a colored LED which is turned on and off via GPIO pins according to the following table:

| Object | LED Color | UART Backlight | PWM Speed |
|------|---------|--------------|------|
| Apple | Red | Red | 50% |
| Grapes | Blue | Blue | 100% |
| Pear | Yellow | Yellow | 50% |
| Pen | Green | Green | 0%(off) |
| Other | Off | Black | 0%(off) |

The pin number to color mappings are configurable from the GPIO [Module Twin Desired Properties](https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-module-twins).  Thus, the same module application can be used for different boards with different pin header layouts.

The UART module displays the name of the fruit on the LCD display and sets the LCD background color to a color that matches the value in the above table.
The PWM module sets the motor speed to the corresponding value in the above table.

The I2C and SPI invensense mpu device modules send out Orientation messages based on the Z-Axis value +/- as RightSideUp and UpsideDown.  
If the GPIO module sees a RightSideUp message it displays the led colors as normal.  if it sees and UpsideDown message it inverts the sense of the LEDS and turns off the LED that corresponds to the recognized fruit and turns all the Other LEDs on.
If the UART module sees a RightSideUp message it displays the background color per the current fruit. if it sees an UpsideDown message it sets the background color to White regardless of the current fruit.
The example deployment in the project routes the I2C mpu6050 to the UART and the SPI mpu9050 to GPIO.

Another issue to be aware of is that for security reasons modules cannot directly read state from other modules.  A consequence of this is that if a module, such as GPIO for example, starts up after the CustomVision WinML module or crashes and recovers, it won't know what the current fruit is until that changes and a new Fruit message is sent out from the WinML module.  In order to solve this state initialization problem all the modules send out a Module Loaded message with their Module ID when they start.  When a module that is a state provider -- currently ML and the mpu modules -- sees a ModuleLoaded message it re-sends it's current state.
The upstream Azure Function Module reflects the module loaded, orientation changed, and fruit messages across all the boards.  This is defined with a combination of the device twin master/slave relationship in the device twin desired properties along with the module type relationship.  The module type relationship is currently a table built into the Function Module code.  Please see the comments in that code for more details.

## Installation

In you Azure Subscription you need a resource group that contains the following resources:

* Azure IoT Hub
* Azure Container Registry
* Azure Storage
* Azure Function App
