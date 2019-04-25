//
// Copyright (c) Microsoft. All rights reserved.
//

namespace SampleModule
{
    using System;
    using Newtonsoft.Json;
    using EdgeModuleSamples.Common;

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
        [JsonProperty(PropertyName = "ambient")]
        public Ambient Ambient { get; } = new Ambient();

        [JsonProperty(PropertyName = "timeCreated")]
        public DateTime TimeCreated { get; set; }

    }


    public class Ambient
    {
        [JsonProperty(PropertyName = "temperature")]
        public double Temperature { get; set; }

        [JsonProperty(PropertyName = "humidity")]
        public double Humidity { get; set; }

    }

}
