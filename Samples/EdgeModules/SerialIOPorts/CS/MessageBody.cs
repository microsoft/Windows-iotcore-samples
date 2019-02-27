// Copyright (c) Microsoft. All rights reserved.

namespace SampleModule
{
    using System;
    using Newtonsoft.Json;
    using EdgeModuleSamples.Common.Logging;

    /// <summary>
    ///Body:
    ///{
    ///  “machine”:{
    ///    “temperature”:,
    ///    “pressure”:
    ///  },
    ///  “ambient”:{
    ///    “temperature”: , 
    ///    “humidity”:
    ///  }
    ///  “timeCreated”:”UTC iso format”
    ///}
    ///Units and types:
    ///Temperature: double, C
    ///Humidity: int, %
    ///Pressure: double, psi
    /// </summary>
    public class MessageBody
    {
        [JsonProperty(PropertyName = "machine")]
        public Machine Machine { get; set; }

        [JsonProperty(PropertyName = "ambient")]
        public Ambient Ambient { get; set; }

        [JsonProperty(PropertyName = "timeCreated")]
        public DateTime TimeCreated { get; set; }

        [JsonIgnore]
        public int Number { get; set; }

        [JsonIgnore]
        public bool isValid {get;private set; } = false;


        [JsonIgnore]
        public string SerialEncode
        {
            get
            {
                return $"{Number:00000}/{Machine.SerialEncode}/{Ambient.SerialEncode}";
            }
            set
            {
                try
                {
                    var values = value.Split('/');
                    Number = int.Parse(values[0]);
                    Machine = new Machine();
                    Machine.SerialEncode = values[1];
                    Ambient = new Ambient();
                    Ambient.SerialEncode = values[2];
                    isValid = true;
                }
                catch (Exception ex)
                {
                    Log.WriteLineException(ex);
                }
            }
        }

        // # of chars in a serial message
        public const int SerialSize = 30;  
    }

    public class Machine
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "pressure")]
        public double Pressure { get; set; }

        [JsonIgnore]
        public string SerialEncode
        {
            get
            {
                return $"{Temperature:000.00},{Pressure:000.00}"; // 13 chars
            }
            set
            {
                try
                {
                    var values = value.Split(',');
                    Temperature = double.Parse(values[0]);
                    Pressure = double.Parse(values[1]);
                }
                catch (Exception ex)
                {
                    Log.WriteLineException(ex);
                }
            }
        }
    }

    public class Ambient
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "humidity")]
        public int Humidity { get; set; }

        [JsonIgnore]
        public string SerialEncode
        {
            get
            {
                return $"{Temperature:000.00},{Humidity:000}"; // 10 chars
            }
            set
            {
                try
                {
                    var values = value.Split(',');
                    Temperature = double.Parse(values[0]);
                    Humidity = int.Parse(values[1]);
                }
                catch (Exception ex)
                {
                    Log.WriteLineException(ex);
                }
            }
        }
    }

}
