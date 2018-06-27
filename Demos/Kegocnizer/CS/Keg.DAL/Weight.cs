using Keg.DAL.Models;
using Sensors.Weight;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using Windows.Devices.Gpio;
using Microsoft.ApplicationInsights.DataContracts;

namespace Keg.DAL
{
    public class Weight
    {
        public IDictionary<string, object> CalibrationSettings { get; set; }
        public const string WeightClockGpioPinNumberSetting = @"WeightClockGPIOPinNumber"; // an integer, such as 5 or 9 or 17
        public const string WeightDataGpioPinNumberSetting = @"WeightDataGPIOPinNumber"; // an integer, such as 5 or 9 or 17
        public const string AdjustWeightFactorSetting = @"AdjustWeightFactor"; // in integer
        public const string AdjustWeightOffsetSetting = @"AdjustWeightOffset"; // an integer
        public const string PreferredWeightUnits = @"PreferredWeightUnits"; // a supported Measurement.UnitsOfMeasure value, like "Pounds"

        //RPI
        const string WEIGHTCLOCKGPIOPINNUMBER = "21"; //PIN 40
        const string WEIGHTDATAGPIOPINNUMBER = "12";  //PIN 32


        //private double calibrationConstant; use AdjustWeightSetting instead
        private HX711 device;
        private GpioPin dataPin;
        private GpioPin clockPin;
        public FixedSizedQueue<Measurement> PriorMeasurements { get; set; }

        public Measurement GetWeight(bool single =false)
        {
            try
            {
                if (clockPin == null || dataPin == null)
                {
                    return null;
                }

                // TODO: sample sensors and report weight -- mocked for now
                //return new Measurement(165.0f, Measurement.UnitsOfMeasure.Pounds);
                device = new HX711(clockPin, dataPin);

                var w = _GetOutputData();
                Debug.WriteLine($"Single:{w}");
                var c = Calibrated(w);

                if(!CheckAnomaly(c))
                {
                    PriorMeasurements.Enqueue(new Measurement(c, Measurement.UnitsOfMeasure.Ounces));
                }
                
                Debug.WriteLine($"Current Avg:{PriorMeasurements.Average(i => i.Amount)}");
#if DEBUG
                foreach (var item in PriorMeasurements)
                {
                    Debug.WriteLine($"   {item.Amount}");
                }
#endif

                if (single)
                    return new Measurement(w, Measurement.UnitsOfMeasure.Ounces);
                else
                    return new Measurement(PriorMeasurements.Average(i => i.Amount), Measurement.UnitsOfMeasure.Ounces);
            }
            catch(Exception ex)
            {
                Debug.WriteLine($"Error,{ex.Message}");
                KegLogger.KegLogException(ex, "Weight:GetWeight", SeverityLevel.Critical);

                KegLogger.KegLogTrace(ex.Message, "Weight:GetWeight", SeverityLevel.Warning,
                    new Dictionary<string, string>() {
                       {"ClockPin", CalibrationSettings[WeightClockGpioPinNumberSetting].ToString() },
                       {"DataPin", CalibrationSettings[WeightDataGpioPinNumberSetting].ToString() }
                   });
            }

            return null;
        }

        private bool CheckAnomaly(float newValue)
        {
            if(PriorMeasurements.Count > 2 && newValue > PriorMeasurements.Average(i => i.Amount) * 2)
            {
                Debug.WriteLine($" Anomaly: {newValue}");
                KegLogger.KegLogTrace("Anomaly Detected!", "Weight:GetWeight", SeverityLevel.Error,
                    new Dictionary<string, string>() {
                       {"Weight", newValue.ToString() }
                   });

                return true;
            } else
            {
                return false;
            }
        }
        private float Calibrated(float w)
        {
            //Debug.WriteLine($"Weight:{w}");
            Debug.WriteLine($"Weight Factor:{CalibrationSettings[AdjustWeightFactorSetting]}, Weight Offset:{CalibrationSettings[AdjustWeightOffsetSetting]}");
            var a = float.Parse(CalibrationSettings[AdjustWeightFactorSetting].ToString());
            var b = float.Parse(CalibrationSettings[AdjustWeightOffsetSetting].ToString()); b = b == 0 ? 1 : b;
            //Debug.WriteLine($"Calibrated:{ (a - (w / b))*100 })");
            //return (a - (w / b))*100;
            Debug.WriteLine($"Calibrated:{ ((w * a) + b) }");
            return (w * a) + b;

        }

        // the purpose of this object is to allow someone an easy way to generate the proper
        // default calibration settings that this sensor interface object supports.
        // in this case, it supports a Measurement that indicates how many degrees to add
        // to readings to account for variations in the tolerance of the actual circuitry.  
        public static IDictionary<string, object> GetDefaultCalibrationSettings()
        {
            return new Dictionary<string, object>
            {
                { AdjustWeightFactorSetting, "1" },
                { AdjustWeightOffsetSetting, "0" },
                { WeightClockGpioPinNumberSetting, WEIGHTCLOCKGPIOPINNUMBER },
                { WeightDataGpioPinNumberSetting, WEIGHTDATAGPIOPINNUMBER },
                { PreferredWeightUnits, "Ounces" }
            };
        }

        public event EventHandler<MeasurementChangedEventArgs> WeightChanged;
        protected virtual void OnWeightChanged(MeasurementChangedEventArgs e)
        {
            WeightChanged?.Invoke(this, e);
        }

        public Weight()
        {
            CalibrationSettings = new Dictionary<string, object>(GetDefaultCalibrationSettings());
            PriorMeasurements = new FixedSizedQueue<Measurement>(10);
        }

        public Weight(IDictionary<string, object> calibrationSettings)
            : this()
        {
            foreach(string key in calibrationSettings.Keys)
            {
                if (key.Equals(WeightClockGpioPinNumberSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(WeightDataGpioPinNumberSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(AdjustWeightFactorSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(AdjustWeightOffsetSetting, StringComparison.OrdinalIgnoreCase)
                    || key.Equals(PreferredWeightUnits, StringComparison.OrdinalIgnoreCase))
                {
                    CalibrationSettings[key] = calibrationSettings[key].ToString();
                }
            }
        }

        public void Initialize()
        {
            if (device == null)
            {
                GpioController controller = GpioController.GetDefault();
                GpioOpenStatus status;

                if (controller != null
                    && controller.TryOpenPin(Int32.Parse(CalibrationSettings[WeightClockGpioPinNumberSetting].ToString()), GpioSharingMode.Exclusive, out clockPin, out status)
                    && controller.TryOpenPin(Int32.Parse(CalibrationSettings[WeightDataGpioPinNumberSetting].ToString()), GpioSharingMode.Exclusive, out dataPin, out status))
                {
                    device = new HX711(clockPin, dataPin);
                }
                else
                    device = null;
            }
            if (device != null)
                device.PowerOn();
        }

        private int _GetOutputData()
        {
            Initialize();
            int result = 0;
            if (device != null)
            {
                result = device.Read();
            }
            device.PowerDown();
            return result;
        }

        //public string GetReading()
        //{
        //    string numberFormat = "G" + Precision;
        //    return LeadingUnit + ((_GetOutputData() - Offset) / CalibrationConstant).ToString(numberFormat) + TrailingUnit;
        //}

        private Timer _timer;
        public void Initialize(int initialDelay, int period)
        {
            if (device != null)
            {
                _timer = new Timer(OnTimer, null, initialDelay, period);
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
            var measurement = GetWeight();
            if(measurement != null )
            {
                Debug.WriteLine($" OnTimer:{measurement.Amount}");
                PriorMeasurements.Enqueue(measurement);
             
                //var result = new Measurement( PriorMeasurements.Average(i => i.Amount), Measurement.UnitsOfMeasure.Ounces);
                this.OnWeightChanged(new MeasurementChangedEventArgs(measurement));
            }
            
        }
    }
}
