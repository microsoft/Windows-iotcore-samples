//
// Copyright (c) Microsoft. All rights reserved.
//
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SampleModule
{
    public class MessageBody
    {
        public LabelResult[] results;
        public Metrics metrics = new Metrics();
    }

    public class LabelResult
    {
        public string label { get; set; }
        public double confidence { get; set; }
    }

    public class Metrics
    {
        public int evaltimeinms { get; set; }        
        public int cycletimeinms { get; set; }        

    }
}
