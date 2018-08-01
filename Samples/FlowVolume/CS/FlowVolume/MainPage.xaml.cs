using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;
using Keg.DAL;
using System.Linq;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace FlowVolume
{

    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private Flow _flow { get; set; }
        private SortedList<DateTime, double> _readings { get; set; } = new SortedList<DateTime, double>();

        public MainPage()
        {
            this.InitializeComponent();

            // to change the calibration numbers, do it here to override the defaults within the code
            var calibration = new Dictionary<string, object>();
            calibration[Flow.FlowCalibrationFactorSetting] = "0.045";
            calibration[Flow.FlowCalibrationOffsetSetting] = "0";

            _flow = new Flow(calibration);
            _flow.FlowChanged += OnFlowChange;
            //_flow.Initialize();  // start without a timer, or ...
            _flow.Initialize(1000, 500); // start with a timer

            LastMeasurement = new Keg.DAL.Models.Measurement(0, Keg.DAL.Models.Measurement.UnitsOfMeasure.Ounces);
            LastMeasurement.PropertyChanged += OnAmountChange;
        }

        public Keg.DAL.Models.Measurement LastMeasurement { get; set; }

        private const int NumberOfValuesToStore = 25;
        private async void OnAmountChange(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Amount")
            {
                Debug.WriteLine("OnAmountChange " + LastMeasurement.Amount);

                _readings.Add(DateTime.Now, LastMeasurement.Amount);
                int lastIndex = _readings.Count-1;

                double average = 0.0;
                if (_readings.Count > 0)
                    average = (_readings.Values[lastIndex] - _readings.Values[0]) / NumberOfValuesToStore;

                if (_readings.Count > NumberOfValuesToStore)
                    _readings.RemoveAt(0);

                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    labelRpms.Text = LastMeasurement.ToString();
                    rpms.Value = average;
                });
            }
        }

        private void OnFlowChange(object sender, MeasurementChangedEventArgs e)
        {
            //lock (LastMeasurement)
            //{
            //    int change = LastMeasurement.CompareTo(e.Measurement);
            //    if (change == 0)
            //        return;
                Debug.WriteLine($"{e.GetType().Name}: {e.Measurement}");
                LastMeasurement.Amount = e.Measurement.Amount;
            //}
        }
    }
}
