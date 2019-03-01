// Copyright (c) Microsoft Corporation. All rights reserved.

using SmartDisplay.Controls;
using SmartDisplay.ViewModels;
using System;
using System.Diagnostics;
using System.Linq;
using Windows.Devices.Gpio;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace SmartDisplay.Views
{
    /// <summary>
    /// Displays Windows Update status of device
    /// </summary>
    public sealed partial class BlinkyPage : PageBase
    {
        public BlinkyPageVM ViewModel { get; } = new BlinkyPageVM();
        protected override BaseViewModel ViewModelImpl => ViewModel;

        public object GpioStatus { get; private set; }

        private int[] LED_PINS = Enumerable.Range(0, 35).ToArray();
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;
        private SolidColorBrush blackBrush = new SolidColorBrush(Windows.UI.Colors.Black);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        public BlinkyPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += Timer_Tick;

            for (int i = 0; i < LED_PINS.Length; i++)
            {
                GPIOPinIndex.Items.Add(LED_PINS[i].ToString());
            }

            try
            {
                InitGPIO();
                if (pin != null)
                {
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to initialize GPIO, " + ex.Message);
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                Debug.WriteLine("There is no GPIO controller on this device.");
                return;
            }

            GpioOpenStatus status;

            int pinIndex = LED_PINS[0];
            if (ApplicationData.Current.LocalSettings.Values.ContainsKey("GPIOIndex"))
            {
                int savedPin;
                if (!int.TryParse(ApplicationData.Current.LocalSettings.Values["GPIOIndex"].ToString(), out savedPin))
                    Debug.WriteLine($"Unable to use the saved GPIOIndex value, '{ApplicationData.Current.LocalSettings.Values["GPIOIndex"]}.");
                pinIndex = savedPin;
            }

            // different pins work on different devices; find one that works
            //while (pinIndex < LED_PINS.Length && !gpio.TryOpenPin(LED_PINS[pinIndex], GpioSharingMode.Exclusive, out pin, out status))
            //    pinIndex++;
            //if (pinIndex >= LED_PINS.Length)
            //{
            //    Debug.WriteLine($"No suitable GPIO pin could be initialized correctly.");
            //    return;
            //}
            if (pin != null)
            {
                pin.Dispose();
                pin = null;
            }
            if (!gpio.TryOpenPin(pinIndex, GpioSharingMode.Exclusive, out pin, out status))
            {
                Debug.WriteLine($"Unable to open GPIO pin {pinIndex}.");
                return;
            }

            try
            {
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
                pin.SetDriveMode(GpioPinDriveMode.Output);
                Debug.WriteLine($"GPIO pin {pinIndex} initialized correctly.");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPIO pin {pinIndex} not initialized successfully, " + ex.Message);
            }
        }

        private void Timer_Tick(object sender, object e)
        {
            try
            {
                if (pinValue == GpioPinValue.High)
                {
                    pinValue = GpioPinValue.Low;
                    LED.Fill = blackBrush;
                }
                else
                {
                    pinValue = GpioPinValue.High;
                    LED.Fill = grayBrush;
                }
                if (pin != null)
                    pin.Write(pinValue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"GPIO pin not blinked successfully, " + ex.Message);
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            if (pin != null)
                pin.Dispose();
            pin = null;
        }

        private void GPIOPinIndex_SelectionChanged_1(object sender, Windows.UI.Xaml.Controls.SelectionChangedEventArgs e)
        {
            ApplicationData.Current.LocalSettings.Values["GPIOIndex"] = GPIOPinIndex.SelectedValue;
            try
            {
                InitGPIO();
                if (pin != null && !timer.IsEnabled)
                {
                    timer.Start();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to initialize GPIO, " + ex.Message);
            }
        }
    }
}
