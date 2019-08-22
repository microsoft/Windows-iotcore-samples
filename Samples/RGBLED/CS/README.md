# RGB LED Sample

In this sample, we will connect a Tri-color LED to Raspberry Pi 2 or 3. The LED will blink changing colors from Red, Blue, and Green.

This is a headed sample, so please ensure that your device is in headed
mode by running this command: `setbootoption.exe headed` (changing the headed/headless state will require a reboot).

Also, be aware that the GPIO APIs are only available on Windows IoT Core, so this sample cannot run on your desktop.


### Components

You will need the following components :

* a [754-1615-ND Tri-color LED](http://www.digikey.com/product-detail/en/WP154A4SUREQBFZGC/754-1615-ND/3084119)

* a [330 &#x2126; resistor](http://www.digikey.com/product-detail/en/CFR-25JB-52-330R/330QBK-ND/1636)

* 2x [100 &#x2126; resistors](http://www.digikey.com/product-detail/en/CFR-25JB-52-100R/100QBK-ND/246)

* a breadboard and several male-to-female and male-to-male connector wires

### Connect to your Device

Let's start by wiring up the components on the breadboard as shown in the diagram below.

![Breadboard connections](../../../Resources/images/RGBLED/RGBLED_bb.png)

<sub>*Image made with [Fritzing](http://fritzing.org/)*</sub>

Here is the schematic:

![Circuit Schematic](../../../Resources/images/RGBLED/RGBLED-schematic_schem.png)

<sub>*Image made with [Fritzing](http://fritzing.org/)*</sub>

The pinout of the Tri-color LED is shown below and can be found in the [datasheet](http://www.kingbrightusa.com/images/catalog/SPEC/WP154A4SUREQBFZGC.pdf)

![Tri-color LED Pinout](../../../Resources/images/RGBLED/RGBLED_Pinout.png)

#### Connecting the Tri-color LED

* Insert the Tri-color LED into the breadboard as shown in the breadboard diagram at the top of the page.

* Connect one end of the 330 &#x2126; resistor to the red lead of the Tri-color LED.

* Connect the other end of the 330 &#x2126; resistor to Pin 29 GPIO5 of the Raspberry Pi 2 or 3.

* Connect one end of a 100 &#x2126; resistor to the blue lead of the Tri-color LED.

* Connect the other end of the 100 &#x2126; resistor to Pin 31 GPIO6 of the Raspberry Pi 2 or 3.

* Connect one end of a 100 &#x2126; resistor to the green lead of the Tri-color LED.

* Connect the other end of the 100 &#x2126; resistor to Pin 33 GPIO13 of the Raspberry Pi 2 or 3.

* Connect the cathode (the longest leg) of the Tri-color LED to Pin 6 GND.

Here is the pinout of the Raspberry Pi 2 and 3:

![Raspberry Pi 2 and 3 pinout](../../../Resources/images/PinMappings/RP2_Pinout.png)

<sub>*Image made with [Fritzing](http://fritzing.org/)*</sub>

### Deploy your app

You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) and navigating to the `samples-develop\RGBLED`.  This sample is written in C#. Make a copy of the folder on your disk and open the project from Visual Studio.

Follow the instructions to [setup remote debugging and deploy the app](/Docs/AppDeployment.htm#csharp). The RGBLED app will deploy and start on the Windows IoT device, and you should see the LED blink in sync with the simulation on the screen.

### Let's look at the code

First, we get the default GPIO controller and check that it's not null.
`GpioController.GetDefault()` will return null on platforms that do not contain
a GPIO controller.

```csharp
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }
```

Next, we open the pins we'll be using later in the program. The RGB LED
requires 3 gpio pins - one to drive each color channel of the LED.
The code takes different behavior depending on which platform it's running on.
If we're running on Raspberry Pi, we use pins 5, 6, and 13 because they're
located next to each other physically on the header. If we're not running on
Raspberry Pi, we take the first 3 available pins. There is also logic to skip
pins connected to onboard functions on known hardware platforms.

```csharp
            var deviceModel = GetDeviceModel();
            if (deviceModel == DeviceModel.RaspberryPi2)
            {
                // Use pin numbers compatible with documentation
                const int RPI2_RED_LED_PIN = 5;
                const int RPI2_GREEN_LED_PIN = 13;
                const int RPI2_BLUE_LED_PIN = 6;

                redpin = gpio.OpenPin(RPI2_RED_LED_PIN);
                greenpin = gpio.OpenPin(RPI2_GREEN_LED_PIN);
                bluepin = gpio.OpenPin(RPI2_BLUE_LED_PIN);
            }
            else
            {
                // take the first 3 available GPIO pins
                var pins = new List<GpioPin>(3);
                for (int pinNumber = 0; pinNumber < gpio.PinCount; pinNumber++)
                {
                    // ignore pins used for onboard LEDs
                    switch (deviceModel)
                    {
                        case DeviceModel.DragonBoard410:
                            if (pinNumber == 21 || pinNumber == 120)
                                continue;
                            break;
                    }

                    GpioPin pin;
                    GpioOpenStatus status;
                    if (gpio.TryOpenPin(pinNumber, GpioSharingMode.Exclusive, out pin, out status))
                    {
                        pins.Add(pin);
                        if (pins.Count == 3)
                        {
                            break;
                        }
                    }
                }

                if (pins.Count != 3)
                {
                    GpioStatus.Text = "Could not find 3 available pins. This sample requires 3 GPIO pins.";
                    return;
                }

                redpin = pins[0];
                greenpin = pins[1];
                bluepin = pins[2];
            }
```

Next, we initialize the pins as outputs driven HIGH, which causes the LED
to be OFF. We also display which pin numbers are in use. If you're
not using Raspberry Pi, hook up the RGB LED to the pins shown on the display.

```csharp
            redpin.Write(GpioPinValue.High);
            redpin.SetDriveMode(GpioPinDriveMode.Output);
            greenpin.Write(GpioPinValue.High);
            greenpin.SetDriveMode(GpioPinDriveMode.Output);
            bluepin.Write(GpioPinValue.High);
            bluepin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = string.Format(
                "Red Pin = {0}, Green Pin = {1}, Blue Pin = {2}",
                redpin.PinNumber,
                greenpin.PinNumber,
                bluepin.PinNumber);
```

Finally, we start a periodic timer which we will use to rotate through the colors
of the LED. We use a `DispatcherTimer` because we'll be updating the UI
on the timer callback. If we did not need to update the UI, it would be better
to use a `System.Threading.Timer` which runs on a separate thread. The less we
can do on the UI thread, the more responsive the UI will be.

```csharp
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;
            timer.Start();
        }
```

In the timer callback, we light up the currently active LED and update the UI.

```csharp
        private void FlipLED()
        {
            Debug.Assert(redpin != null && bluepin != null && greenpin != null);

            switch (ledStatus)
            {
                case LedStatus.Red:
                    //turn on red
                    redpin.Write(GpioPinValue.High);
                    bluepin.Write(GpioPinValue.Low);
                    greenpin.Write(GpioPinValue.Low);

                    LED.Fill = redBrush;
                    ledStatus = LedStatus.Green;    // go to next state
                    break;
                case LedStatus.Green:

                    //turn on green
                    redpin.Write(GpioPinValue.Low);
                    greenpin.Write(GpioPinValue.High);
                    bluepin.Write(GpioPinValue.Low);

                    LED.Fill = greenBrush;
                    ledStatus = LedStatus.Blue;     // go to next state
                    break;
                case LedStatus.Blue:
                    //turn on blue
                    redpin.Write(GpioPinValue.Low);
                    greenpin.Write(GpioPinValue.Low);
                    bluepin.Write(GpioPinValue.High);

                    LED.Fill = blueBrush;
                    ledStatus = LedStatus.Red;      // go to next state
                    break;
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            FlipLED();
        }
```
