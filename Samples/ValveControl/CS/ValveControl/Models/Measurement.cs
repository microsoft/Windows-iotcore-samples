using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Keg.DAL.Models
{
    public class Measurement : IComparable<Measurement>, INotifyPropertyChanged
    {
        public enum UnitsOfMeasure { Pounds, Ounces, Kilograms, Milliliters, Celsius, Fahrenheit }

        private float _amount;
        [JsonProperty("amount")]
        public float Amount
        {
            get { return _amount; }
            set { _amount = value; OnPropertyChanged(); }
        }

        private UnitsOfMeasure _units;
        [JsonProperty("units")]
        public UnitsOfMeasure Units
        {
            get { return _units; }
            set { _units = value; OnPropertyChanged(); }
        }

        [JsonProperty("timestamp")]
        public DateTime Timestamp { get; set; }

        public Measurement() { }

        public Measurement(float amount, UnitsOfMeasure units)
        {
            Amount = amount;
            Units = units;
            Timestamp = DateTime.Now;
        }

        public Measurement(float amount, UnitsOfMeasure units, DateTime timestamp)
            : this(amount, units)
        {
            Timestamp = timestamp;
        }

        public event PropertyChangedEventHandler PropertyChanged = delegate { };

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            //Debug.WriteLine($"OnPropertyChanged, {propertyName} " + (PropertyChanged == null ? "false" : "true"));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public override String ToString()
        {
            return $"{Amount} {Units}";
        }

        // if difference is greater than the predefined tolerance 0.001f, return -1 if less than, 1 if greater than.  If within the tolerance, return 0.
        public int CompareTo(Measurement other)
        {
            if (Units != other.Units)
                throw new ArgumentException("Can only compare to objects using the same unit of measurement.");
            float precision = 0.001f;
            float difference = Math.Abs(other.Amount - Amount);
            if (difference < precision)
                return 0;
            return 1; // Amount.CompareTo(other.Amount);
        }
    }
}
