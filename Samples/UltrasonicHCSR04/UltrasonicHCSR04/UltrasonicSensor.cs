using System.Diagnostics;
using Windows.Devices.Gpio;

public class UltrasonicSensor
{
    private const int PIN_DRIVER = 27;
    private const int PIN_ECHO = 17;
    private GpioPin driverPin;
    private GpioPin echoPin;
    private GpioController gpioController;

    public double lastDelay = 0.0;
    public double riseTime = 0.0;
    private Stopwatch stopwatch;

    public UltrasonicSensor()
    {
        InitGpio();
    }

    private void InitGpio()
    {
        gpioController = GpioController.GetDefault();
        if (gpioController == null)
        {
            return;
        }
        driverPin = gpioController.OpenPin(PIN_DRIVER);
        echoPin = gpioController.OpenPin(PIN_ECHO);
        driverPin.Write(GpioPinValue.Low);
        driverPin.SetDriveMode(GpioPinDriveMode.Output);
        echoPin.SetDriveMode(GpioPinDriveMode.InputPullDown);
        stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
    }

    public void Trigger()
    {
        // trigger an echo
        driverPin.Write(GpioPinValue.High);
        Wait(0.1); // datasheet says 10us would do but this too is fine
        driverPin.Write(GpioPinValue.Low);
        stopwatch.Restart();
        Wait(0.1); // let the output settle
        while (stopwatch.ElapsedMilliseconds < 400 && echoPin.Read() == GpioPinValue.Low) { } // busy wait to make sure output is still low
        riseTime = stopwatch.ElapsedTicks;
        while (stopwatch.ElapsedMilliseconds < 400 && echoPin.Read() == GpioPinValue.High) { } // wait until the output falls again
        lastDelay = (stopwatch.ElapsedTicks - riseTime) / Stopwatch.Frequency;
    }

    public double GetCentimeters()
    {
        return 100.0 * lastDelay * 340.0 / 2.0; // sound speed 340m/s, divide by two as it's both ways
    }

    // busy wait for milliseconds
    // source: http://www.iot-developer.net/windows-iot/uwp-programming-in-c/timer-and-timing/stopwatch
    private void Wait(double milliseconds)
    {
        long initialTick = stopwatch.ElapsedTicks;
        long initialElapsed = stopwatch.ElapsedMilliseconds;
        double desiredTicks = milliseconds / 1000.0 * Stopwatch.Frequency;
        double finalTick = initialTick + desiredTicks;
        while (stopwatch.ElapsedTicks < finalTick)
        {

        }
    }
}