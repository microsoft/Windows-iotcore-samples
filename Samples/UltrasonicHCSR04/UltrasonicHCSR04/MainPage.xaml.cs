using System;
using System.Threading;
using Windows.UI.Core;
using Windows.UI.Xaml.Controls;

namespace UltrasonicHCSR04
{
    public sealed partial class MainPage : Page
    {
        private System.Threading.Timer timer;
        private UltrasonicSensor sensor;

        public MainPage()
        {
            this.InitializeComponent();
            sensor = new UltrasonicSensor();
            timer = new Timer(new TimerCallback(TimerTickAsync), null, 750, 750);

        }

        private async void TimerTickAsync(object timerState)
        {
            // invoke the timer on UI thread
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
             {
                 // measure once -- this is a blocking call
                 sensor.Trigger();
                 SensorValue.Text = String.Format("Distance: {0:F1} centimeters", sensor.GetCentimeters());
             });
        }
    }
}
