---
page_type: sample
urlFragment: ultrasonic-hcsr04
languages:
  - csharp
products:
  - windows
description: This sample demonstrates using the HC-SR04 ultrasonic sensor with Window 10 IoT Core.
---

# HC-SR04 Ultrasonic sensor

This sample demonstrates using the HC-SR04 ultrasonic sensor. It is driven with a trigger signal
and provides a pulse output, pulse width equaling the time to the echo.

Connect the HC-SR04 to the GPIO 5V and GND pins. Trigger signal can be connected to a GPIO output.
Echo signal should be connected through a voltage divider to make sure the echo signal doesn't exceed
3.3V. I used 3k and 5k6 resistors.

This sample is developed and tested using a Raspberri Pi Model 3.

## Additional resources
* [HC-SR04 Datasheet](https://cdn.sparkfun.com/assets/b/3/0/b/a/DGCH-RED_datasheet.pdf)

---

Author: [Ossi Väänänen](https://github.com/oh6hay/)
