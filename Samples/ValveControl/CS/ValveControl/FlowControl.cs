using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;

namespace Keg.DAL
{
    public class FlowControl
    {
        public IDictionary<string, object> CalibrationSettings { get; set; }
        public const string FlowControlGpioPinNumberSetting = @"FlowControlGPIOPinNumber"; // an integer, like 18
        private GpioPin _pin;
        private GpioPinValue _isActive;

        //RPI
        private static readonly Int32 FLOWCONTROLGPIOPIN = 18; //PIN 12

        public bool IsActive
        {
            get { return _isActive == GpioPinValue.High; }
            set
            {
                if ((_isActive == GpioPinValue.High) != value || _pin == null)
                {
                    _isActive = value ? GpioPinValue.High : GpioPinValue.Low;

                    try
                    {
                        if (_pin == null)
                        {
                            int pinNumber = FLOWCONTROLGPIOPIN; // default
                            if (CalibrationSettings.ContainsKey(FlowControlGpioPinNumberSetting))
                                pinNumber = Int32.Parse(CalibrationSettings[FlowControlGpioPinNumberSetting].ToString());
                            Debug.WriteLine($"Flow Control:Initializing pin {pinNumber}.");
                            var c = GpioController.GetDefault();
                            if (c != null)
                            {
                                _pin = c.OpenPin(pinNumber);
                                _pin.SetDriveMode(GpioPinDriveMode.Output);
                            }
                        }
                        if (_pin != null)
                        {
                            Debug.WriteLine($"Setting flow control to {_isActive}.");
                            _pin.Write(_isActive);
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Flow Control:Error setting flow control value, " + ex.Message);
                    }
                }
            }
        }

        public event EventHandler<FlowControlChangedEventArgs> FlowControlChanged;
        protected virtual void OnFlowControlChanged(FlowControlChangedEventArgs e)
        {
            FlowControlChanged?.Invoke(this, e);
        }

        // the purpose of this object is to allow someone an easy way to generate the proper
        // default calibration settings that this sensor interface object supports.
        // in this case, it supports a Measurement that indicates how many degrees to add
        // to readings to account for variations in the tolerance of the actual circuitry.  
        public static IDictionary<string, object> GetDefaultCalibrationSettings()
        {
            return new Dictionary<string, object>
            {
                { FlowControlGpioPinNumberSetting, FLOWCONTROLGPIOPIN.ToString() },
            };
        }

        public FlowControl()
        {
            CalibrationSettings = new Dictionary<string, object>();
        }

        public FlowControl(IDictionary<string, object> calibrationSettings)
            : this()
        {
            CalibrationSettings.Add(FlowControlGpioPinNumberSetting, (calibrationSettings.ContainsKey(FlowControlGpioPinNumberSetting)) ? calibrationSettings[FlowControlGpioPinNumberSetting] as string : "");
        }

        private Timer _timer;
        public void Initialize(int initialDelay, int period)
        {
            Initialize();
            _timer = new Timer(OnTimer, null, initialDelay, period);
        }

        public void Initialize()
        {
            var gpio = GpioController.GetDefault();
            if (gpio == null)
            {
                _pin = null;
                Debug.WriteLine("Flow Control:Unable to initialize, no GPIO controller.");
                return;
            }

            if(_pin == null)
            {
                try
                {
                    _pin = gpio.OpenPin(Int32.Parse(CalibrationSettings[FlowControlGpioPinNumberSetting].ToString()), GpioSharingMode.Exclusive);
                    _pin.Write(_isActive);
                    _pin.SetDriveMode(GpioPinDriveMode.Output);
                }
                catch(Exception ex)
                {
                   Debug.WriteLine($"Flow Control:Unable to initialize, PIN Null.{ex.Message}");
                }
            }
            
        }

        public void Dispose()
        {
            var timer = _timer;
            _timer = null;
            timer?.Dispose();
        }

        private void OnTimer(object state)
        {
            OnFlowControlChanged(new FlowControlChangedEventArgs(IsActive));
        }
    }
}
