using System;

namespace Keg.DAL
{
    public class MeasurementChangedEventArgs : EventArgs
    {
        public Models.Measurement Measurement { get; set; }
        public MeasurementChangedEventArgs(Models.Measurement measurement)
        {
            this.Measurement = measurement;
        }
    }
}