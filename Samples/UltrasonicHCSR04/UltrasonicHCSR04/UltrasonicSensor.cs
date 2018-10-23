using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.Foundation;

public class UltrasonicSensor
{
    private const int PIN_DRIVER = 27;
    private const int PIN_ECHO = 17;
    private GpioPin driverPin;
    private GpioPin echoPin;
    private GpioController gpioController;
    private GpioChangeReader changeReader;

    private double lastDelay = 0.0;
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

        changeReader = new GpioChangeReader(echoPin);
        changeReader.Polarity = GpioChangePolarity.Both; // one measurement results in one rising and one falling edge
        changeReader.Start();

        // we use the stopwatch to time the trigger pulse
        stopwatch = Stopwatch.StartNew();
        stopwatch.Start();
    }

    public void Trigger()
    {
        changeReader.Clear();
        var a = changeReader.WaitForItemsAsync(2); // two edges, measure the time between
        a.Completed = GpioValueChanged;

        // trigger an echo
        driverPin.Write(GpioPinValue.High);
        Wait(0.1); // datasheet says 10us would do but this too is fine
        driverPin.Write(GpioPinValue.Low);
    }

    void GpioValueChanged(IAsyncAction action, AsyncStatus status) {
        var events = changeReader.GetAllItems();
        if (events.Count < 2)
        {
            // fail silently
            return;
        }
        var first = events[0];
        var second = events[1];
        lastDelay = (second.RelativeTime - first.RelativeTime).TotalSeconds;
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
        while (stopwatch.ElapsedTicks < finalTick) ; // busy wait
    }
}