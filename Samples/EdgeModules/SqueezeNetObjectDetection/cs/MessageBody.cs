using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace SqueezeNetObjectDetectionNC
{
    public class MessageBody
    {
        public LabelResult[] results;
    }

    public class LabelResult
    {
        public string label { get; set; }
        public double confidence { get; set; }
    }
}
