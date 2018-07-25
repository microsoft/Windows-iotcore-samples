using Keg.DAL.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.DAL
{
    public class Flow
    {
        public IDictionary<string, object> CalibrationSettings { get; set; }
        public const string FlowGPIOPinNumberSetting = @"FlowGPIOPinNumber"; // a string, valued e.g., SPI0 or SPI1 to indicate which SPI bus is being used
        public const string FlowCalibrationFactorSetting = @"FlowCalibrationFactor"; // a float (as string), such as "0.05" for 'factor' in (reading * factor) + offset
        public const string FlowCalibrationOffsetSetting = @"FlowCalibrationOffset"; // a float (as string), such as "14.0" for 'offset' in (reading * factor) + offset
        private Measurement lastMeasurement; // this value accumulates over time.  Consumers should note the starting measurement and then compare that to a future measurement.

        //RPI
        private static readonly Int32 FLOWGPIOPIN = 3; //PIN5

        public Models.Measurement GetFlow()
        {
            return lastMeasurement;
        }

        public event EventHandler<MeasurementChangedEventArgs> FlowChanged;
        protected virtual void OnFlowChanged(MeasurementChangedEventArgs e)
        {
            FlowChanged?.Invoke(this, e);
        }

        public Flow()
        {
            CalibrationSettings = new Dictionary<string, object>(GetDefaultCalibrationSettings());
            lastMeasurement = new Measurement(0.0f, Measurement.UnitsOfMeasure.Ounces);
        }

        public Flow(IDictionary<string, object> calibrationSettings)
            : this()
        {
            foreach (string key in calibrationSettings.Keys)
            {
                if (key.Equals(FlowGPIOPinNumberSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(FlowCalibrationFactorSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(FlowCalibrationOffsetSetting, StringComparison.OrdinalIgnoreCase))
                {
                    CalibrationSettings[key] = calibrationSettings[key].ToString();
                }
            }
        }

        // the purpose of this object is to allow someone an easy way to generate the proper
        // default calibration settings that this sensor interface object supports.
        // in this case, it supports a Measurement that indicates how many degrees to add
        // to readings to account for variations in the tolerance of the actual circuitry.  
        public static IDictionary<string, object> GetDefaultCalibrationSettings()
        {
            return new Dictionary<string, object>
            {
                { FlowGPIOPinNumberSetting, FLOWGPIOPIN.ToString() },
                { FlowCalibrationFactorSetting, ".0045" },
                { FlowCalibrationOffsetSetting, "0" }
            };
        }

        private GpioPin _pin;

        private bool IsInitialized { get; set; }
        public void Initialize()
        {
            if (IsInitialized)
                return;
            //IsInitialized = true;

            int pinNumber = Int32.Parse(CalibrationSettings[FlowGPIOPinNumberSetting].ToString());

            var c = GpioController.GetDefault();
            if (c != null)
            {
                try
                {
                    _pin = c.OpenPin(pinNumber);
                    if (_pin != null)
                    {
                        if (_pin.IsDriveModeSupported(GpioPinDriveMode.InputPullUp))
                            _pin.SetDriveMode(GpioPinDriveMode.InputPullUp);

                        _pin.ValueChanged += _pin_ValueChanged;
                    }
                }
                catch(Exception ex)
                {
                    Debug.WriteLine($"Exception:{ex.Message}");
                    KegLogger.KegLogException(ex, "Flow:Initialize", SeverityLevel.Critical);

                    KegLogger.KegLogTrace(ex.Message, "Flow:Initialize", SeverityLevel.Error, 
                        new Dictionary<string, string>() {
                       {"PinNumber", CalibrationSettings[FlowGPIOPinNumberSetting].ToString() }
                   });
                    //Pin used exception
                    //TODO
                }
            }

            IsInitialized = true;
        }

        private int _second = -1;
        private float _persecond = 0.0f;

        private void _pin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            Debug.WriteLine($"Flow Pinchanged:{args.Edge}");

            var task = Task.Run(() => {
                if (args.Edge == GpioPinEdge.RisingEdge)
                {
                    int second = DateTime.Now.Second;
                    if (second != _second)
                    {
                        Debug.WriteLine($"{_persecond} per-second");
                        _second = second;
                        int offset = Int32.Parse(CalibrationSettings[FlowCalibrationOffsetSetting].ToString());
                        float factor = float.Parse(CalibrationSettings[FlowCalibrationFactorSetting].ToString());
                        lastMeasurement.Amount += _persecond * factor + offset;

                        _persecond = 0.0f;

                        OnFlowChanged(new MeasurementChangedEventArgs(lastMeasurement));
                        //_second = second;
                        //_persecond = 0.0f;
                    }
                    _persecond++;
                }
            });
        }

        private Timer _timer;
        public void Initialize(int initialDelay, int period)
        {
            Initialize();
            _timer = new Timer(OnTimer, null, initialDelay, period);
        }

        public void ResetFlow()
        {
            lastMeasurement.Amount = 0.0f;
        }
        public void Dispose()
        {
            var timer = _timer;
            _timer = null;
            timer?.Dispose();
        }

        private void OnTimer(object state)
        {
            OnFlowChanged(new MeasurementChangedEventArgs(lastMeasurement));
        }
    }
}
