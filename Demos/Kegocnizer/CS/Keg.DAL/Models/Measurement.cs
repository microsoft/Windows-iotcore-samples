using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Keg.DAL.Models
{
    public class Measurement
    {
        public enum UnitsOfMeasure { Pounds, Ounces, Kilograms, Milliliters, Celsius, Fahrenheit }

        [JsonProperty("amount")]
        public float Amount { get; set; }

        [JsonProperty("units")]
        public UnitsOfMeasure Units { get; set; }

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

        public override String ToString()
        {
            return $"{Amount} {Units}";
        }
    }
}
