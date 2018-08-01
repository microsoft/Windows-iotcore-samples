using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Keg.DAL.Models;
using Windows.Devices.Gpio;
using Sensors.Temperature;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.DAL
{
    public class Temperature : IDisposable
    {
        //RPI
        private static readonly Int32 TEMPERATUREDATAPIN = 4; //PIN 7
        //MBM
        //const Int32 TEMPERATUREDATAPIN = 9;

        public IDictionary<string, object> CalibrationSettings { get; set; }
        public const string AdjustTemperatureSetting = @"AdjustTemperature"; // a Measurement object, such as {-2, Fahrenheit}
        public const string TempGpioPinNumberSetting = @"TempGPIOPinNumber"; // an integer, such as 5 or 9 or 17
        public const string PreferredTemperatureUnits = @"TemperatureUnits"; // a Measurement.Units enum value, such as "Celsius" or "Fahrenheit"
        
        private GpioPin _pin;
        private Dht11 _dht11;
        private Measurement _lastMeasurement;

        // takes an instant reading or returns the average accumulated value, and returns
        // the interpreted value adjusted by calibration settings.
        public async Task<Models.Measurement> GetTemperature()
        {
            Measurement result = _lastMeasurement;

            try
            {
                if (_pin == null || _dht11 == null)
                {
                    int pinNumber = TEMPERATUREDATAPIN; // default
                    if (CalibrationSettings.ContainsKey(TempGpioPinNumberSetting))
                        pinNumber = Int32.Parse(CalibrationSettings[TempGpioPinNumberSetting].ToString());
                    var c = GpioController.GetDefault();
                    if (c != null)
                    {
                        _pin = c.OpenPin(pinNumber, GpioSharingMode.Exclusive);
                        if (_pin != null)
                            _dht11 = new Dht11(_pin, GpioPinDriveMode.Input);
                    }
                }
                if(_dht11 == null )
                {
                    return null;
                }

                DhtReading reading = await _dht11.GetReadingAsync().AsTask();
                for (int retries = 0; retries < 10; retries++)
                {
                    if (reading.TimedOut)
                        Debug.Write(".");
                    else if (!reading.IsValid)
                        Debug.Write("x");
                    else
                        break;
                }
                if (reading.IsValid)
                {
                    //Debug.WriteLine($"Temp reading = {reading.Temperature}");
                    Measurement.UnitsOfMeasure units = Measurement.UnitsOfMeasure.Fahrenheit;
                    if (CalibrationSettings.ContainsKey(PreferredTemperatureUnits))
                        units = (Measurement.UnitsOfMeasure)Enum.Parse(typeof(Measurement.UnitsOfMeasure), CalibrationSettings[PreferredTemperatureUnits].ToString());
                    Measurement adjust = new Measurement(0.0f, Measurement.UnitsOfMeasure.Fahrenheit);
                    if (CalibrationSettings.ContainsKey(AdjustTemperatureSetting))
                        adjust = CalibrationSettings[AdjustTemperatureSetting] as Measurement;

                    float temperature = (float)reading.Temperature;
                    temperature = Convert(Measurement.UnitsOfMeasure.Celsius, units, temperature);
                    adjust.Amount = Convert(adjust.Units, units, adjust.Amount);
                    adjust.Units = units;
                    temperature += adjust.Amount; // now that we know they are in the same (preferred) units

                    result = new Measurement(temperature, units);
                    _lastMeasurement = result;
                    OnTemperatureChanged(new MeasurementChangedEventArgs(result));
                }
                else
                {
                    KegLogger.KegLogException(new TemperatureException("Custom: Unable to Read temperature."), "GetTemperature", SeverityLevel.Warning);
                    Debug.WriteLine($"Unable to read temperature.");
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Error, " + ex.Message);
                KegLogger.KegLogException(ex, "GetTemperature", SeverityLevel.Critical);
            }

            return result;
        }

        private float Convert(Measurement.UnitsOfMeasure from, Measurement.UnitsOfMeasure to, float value)
        {
            float result = 0.0f;
            if (from == to)
                result = value;
            else if (from == Measurement.UnitsOfMeasure.Celsius)
                result = value * 9 / 5 + 32;
            else
                result = (value - 32) * 5 / 9;
            return result;
        }

        public event EventHandler<MeasurementChangedEventArgs> TemperatureChanged;
        protected virtual void OnTemperatureChanged(MeasurementChangedEventArgs e)
        {
            TemperatureChanged?.Invoke(this, e);
        }

        // the purpose of this object is to allow someone an easy way to generate the proper
        // default calibration settings that this sensor interface object supports.
        // in this case, it supports a Measurement that indicates how many degrees to add
        // to readings to account for variations in the tolerance of the actual circuitry.  
        public static IDictionary<string,object> GetDefaultCalibrationSettings()
        {
            return new Dictionary<string, object>
            {
                { AdjustTemperatureSetting, new Measurement(0, Measurement.UnitsOfMeasure.Fahrenheit) },
                { TempGpioPinNumberSetting, TEMPERATUREDATAPIN.ToString() },
                { PreferredTemperatureUnits, "Fahrenheit" }
            };
        }

        // default constructor 
        public Temperature()
        {
            CalibrationSettings = new Dictionary<string, object>();
        }

        public Temperature(IDictionary<string,object> calibrationSettings)
            : this()
        {
            //AdjustTemperatureSetting
            CalibrationSettings.Add(AdjustTemperatureSetting, (calibrationSettings.ContainsKey(AdjustTemperatureSetting)) ? calibrationSettings[AdjustTemperatureSetting] as Measurement : new Measurement(0.0f, Measurement.UnitsOfMeasure.Celsius));
            //TempGpioPinNumberSetting
            CalibrationSettings.Add(TempGpioPinNumberSetting, (calibrationSettings.ContainsKey(TempGpioPinNumberSetting)) ? calibrationSettings[TempGpioPinNumberSetting] : TEMPERATUREDATAPIN);
            //PreferredTemperatureUnits
            CalibrationSettings.Add(PreferredTemperatureUnits, (calibrationSettings.ContainsKey(PreferredTemperatureUnits)) ? calibrationSettings[PreferredTemperatureUnits] : Measurement.UnitsOfMeasure.Celsius.ToString());

        }

        private Timer _timer;
        public void Initialize(int initialDelay, int period)
        {
            _timer = new Timer(OnTimer, null, initialDelay, period);
        }

        public void Dispose()
        {
            var timer = _timer;
            _timer = null;
            timer?.Dispose();
        }

        private async void OnTimer(object state)
        {
            OnTemperatureChanged(new MeasurementChangedEventArgs(await GetTemperature()));
        }
    }


    [Serializable()]
    public class TemperatureException : System.Exception
    {
        public TemperatureException() : base() { }
        public TemperatureException(string message) : base(message) { }
        public TemperatureException(string message, System.Exception inner) : base(message, inner) { }

        protected TemperatureException(System.Runtime.Serialization.SerializationInfo info,
            System.Runtime.Serialization.StreamingContext context)
        {

        }
    }

}
