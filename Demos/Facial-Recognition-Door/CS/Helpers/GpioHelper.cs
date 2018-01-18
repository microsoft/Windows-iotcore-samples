using System;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace FacialRecognitionDoor.Helpers
{
    /// <summary>
    /// Interacts with device GPIO controller in order to control door lock and monitor doorbell
    /// </summary>
    public class GpioHelper
    {
        private GpioController gpioController;
        private GpioPin doorbellPin;
        private GpioPin doorLockPin;

        /// <summary>
        /// Attempts to initialize Gpio for application. This includes doorbell interaction and locking/unlccking of door.
        /// Returns true if initialization is successful and Gpio can be utilized. Returns false otherwise.
        /// </summary>
        public bool Initialize()
        {
            // Gets the GpioController
            gpioController = GpioController.GetDefault();
            if(gpioController == null)
            {
                // There is no Gpio Controller on this device, return false.
                return false;
            }

            // Opens the GPIO pin that interacts with the doorbel button
            doorbellPin = gpioController.OpenPin(GpioConstants.ButtonPinID);

            if (doorbellPin == null)
            {
                // Pin wasn't opened properly, return false
                return false;
            }

            // Set a debounce timeout to filter out switch bounce noise from a button press
            doorbellPin.DebounceTimeout = TimeSpan.FromMilliseconds(25);

            if (doorbellPin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
            {
                // Take advantage of built in pull-up resistors of Raspberry Pi 2 and DragonBoard 410c
                doorbellPin.SetDriveMode(GpioPinDriveMode.InputPullUp);
            }
            else
            {
                // MBM does not support PullUp as it does not have built in pull-up resistors 
                doorbellPin.SetDriveMode(GpioPinDriveMode.Input);
            }

            // Opens the GPIO pin that interacts with the door lock system
            doorLockPin = gpioController.OpenPin(GpioConstants.DoorLockPinID);
            if(doorLockPin == null)
            {
                // Pin wasn't opened properly, return false
                return false;
            }
            // Sets doorbell pin drive mode to output as pin will be used to output information to lock
            doorLockPin.SetDriveMode(GpioPinDriveMode.Output);
            // Initializes pin to high voltage. This locks the door. 
            doorLockPin.Write(GpioPinValue.High);

            //Initialization was successfull, return true
            return true;
        }

        /// <summary>
        /// Returns the GpioPin that handles the doorbell button. Intended to be used in order to setup event handler when user pressed Doorbell.
        /// </summary>
        public GpioPin GetDoorBellPin()
        {
            return doorbellPin;
        }

        /// <summary>
        /// Unlocks door for time specified in GpioConstants class
        /// </summary>
        public async void UnlockDoor()
        {
            // Unlock door
            doorLockPin.Write(GpioPinValue.Low);
            // Wait for specified length
            await Task.Delay(TimeSpan.FromSeconds(GpioConstants.DoorLockOpenDurationSeconds));
            // Lock door
            doorLockPin.Write(GpioPinValue.High);
        }

    }
}
