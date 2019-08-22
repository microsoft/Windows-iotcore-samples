# "Hello, blinky!" background service

We'll create a simple Blinky app and connect a LED to your Windows IoT Core device (Raspberry Pi 2 or 3, Up Squared or DragonBoard).  Be aware that the GPIO APIs are
only available on Windows IoT Core, so this sample cannot run on your desktop.

## Headless mode
___

This application is designed for a headless device.  To better understand what Headless mode is and how to configure your device to be headless, follow the instructions [here](https://docs.microsoft.com/en-us/windows/iot-core/learn-about-hardware/headlessmode).

## Load the project in Visual Studio
___

You can find the source code for this sample by downloading a zip of all of our samples [here](https://github.com/Microsoft/Windows-iotcore-samples/archive/master.zip) and navigating to the `samples-develop\HelloBlinkyBackground`.  The sample code is available in either C++ or C#, however the documentation here only details the C# variant. Make a copy of the folder on your disk and open the project from Visual Studio.

Make sure you connect the LED to your board. Go back to the basic 'Blinky' [sample](https://github.com/Microsoft/Windows-iotcore-samples/tree/master/Samples/HelloBlinky) if you need guidance.

Note that the app will not run successfully if it cannot find any available GPIO ports.

Once the project is open and builds, the next step is to [deploy](https://github.com/MicrosoftDocs/windows-iotcore-docs/blob/master/windows-iotcore/develop-your-app/AppDeployment.md) the application to your device.

When everything is set up, you should be able to press F5 from Visual Studio.  The Blinky app will deploy and start on the Windows IoT device, and you should see the attached LED blink.

## Let's look at the code
___
The code for this sample is pretty simple. We use a timer, and each time the 'Tick' event is called, we flip the state of the LED.


## Timer code
___
Here is how you set up the timer in C#:
```csharp
using Windows.System.Threading;

BackgroundTaskDeferral _deferral;
public void Run(IBackgroundTaskInstance taskInstance)
{
    _deferral = taskInstance.GetDeferral();

    this.timer = ThreadPoolTimer.CreatePeriodicTimer(Timer_Tick, TimeSpan.FromMilliseconds(500));
    . . .
}

private void Timer_Tick(ThreadPoolTimer timer)
{
    . . .
}
```


## Initialize the GPIO pin
___
To drive the GPIO pin, first we need to initialize it. Here is the C# code (notice how we leverage the new WinRT classes in the Windows.Devices.Gpio namespace):

```csharp
using Windows.Devices.Gpio;

private void InitGPIO()
{
    var gpio = GpioController.GetDefault();

    if (gpio == null)
    {
        pin = null;
        return;
    }

    pin = gpio.OpenPin(LED_PIN);

    if (pin == null)
    {
        return;
    }

    pin.Write(GpioPinValue.High);
    pin.SetDriveMode(GpioPinDriveMode.Output);
}
```

Let's break this down a little:

* First, we use `GpioController.GetDefault()` to get the GPIO controller.

* If the device does not have a GPIO controller, this function will return `null`.

* Then we attempt to open the pin by calling `GpioController.OpenPin()` with the `LED_PIN` value.

* Once we have the `pin`, we set it to be off (High) by default using the `GpioPin.Write()` function.

* We also set the `pin` to run in output mode using the `GpioPin.SetDriveMode()` function.


## Modify the state of the GPIO pin
___
Once we have access to the `GpioOutputPin` instance, it's trivial to change the state of the pin to turn the LED on or off.  You can modify 'Timer_Tick' to do this.

To turn the LED on, simply write the value `GpioPinValue.Low` to the pin:

```csharp
this.pin.Write(GpioPinValue.Low);
```

and of course, write `GpioPinValue.High` to turn the LED off:

```csharp
this.pin.Write(GpioPinValue.High);
```

Remember that we connected the other end of the LED to the 3.3 Volts power supply, so we need to drive the pin to low to have current flow into the LED.
