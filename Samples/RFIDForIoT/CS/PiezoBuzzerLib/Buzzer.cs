using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace PiezoBuzzerLib
{
    public class Buzzer
    {
        public const int PIEZO_PIN = 24;
        public GpioPin pin;

        public GpioPin InitPiezoGPIO()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                pin = null;
                return null;
            }
            try
            {
                pin = gpio.OpenPin(PIEZO_PIN);
            }
            catch (Exception ex)
            {
                throw new Exception("GPIO initialization failed", ex);
            }
            pin.SetDriveMode(GpioPinDriveMode.Output);
            pin.Write(GpioPinValue.Low);
            return pin;
        }

        public async void Buzz(GpioPin pin)
        {
            pin.Write(GpioPinValue.High);
            await Task.Delay(500);
            pin.Write(GpioPinValue.Low);
        }

        public void StopBuzz(GpioPin pin)
        {
            pin.Write(GpioPinValue.Low);
        }
    }
}
