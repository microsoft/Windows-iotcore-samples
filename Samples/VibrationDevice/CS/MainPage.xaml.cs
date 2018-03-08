using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Haptics;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace VibrationCSApp
{
    public sealed partial class MainPage : Page
    {
        private bool _isCheckedForVibrationDevice = false;
        private CoreDispatcher _coreDispatcher = null;
        private VibrationDevice _vibrationDevice = null;
        private SimpleHapticsControllerFeedback _buzzFeedback = null;

        public MainPage()
        {
            this.InitializeComponent();
            _coreDispatcher = CoreWindow.GetForCurrentThread().Dispatcher;
        }

        private async Task GetVbrationDevice()
        {
            _isCheckedForVibrationDevice = true;

            // Requesting access and updating UI must happen on the UI thread
            await _coreDispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
            {
                var accessStatus = await VibrationDevice.RequestAccessAsync();
                Debug.WriteLine($"Vibration Access: {accessStatus}.");

                if (accessStatus != VibrationAccessStatus.Allowed)
                {
                    Status.Text = $"Vibration Access denied: {accessStatus}. ";

                    if (accessStatus == VibrationAccessStatus.DeniedByUser)
                    {
                        Status.Text += "Please check UI settings and make sure vibration is turned on.";
                    }

                    // Nothing else to do
                    return;
                }

                _vibrationDevice = await VibrationDevice.GetDefaultAsync();
                var status = $"Vibration device {(_vibrationDevice == null ? "NOT" : _vibrationDevice.Id)} found. ";
                Debug.WriteLine(status);
                Status.Text = status;

                if (_vibrationDevice != null)
                {
                    _buzzFeedback = FindFeedback();
                    status = $"Buzz feedback {(_buzzFeedback == null ? "NOT" : "")} supported by this device.";
                    Debug.WriteLine(status);
                    Status.Text += status;
                }
            });
        }

        private SimpleHapticsControllerFeedback FindFeedback()
        {
            foreach (var feedback in _vibrationDevice.SimpleHapticsController.SupportedFeedback)
            {
                // BuzzContinuous feedback is equivalent to vibration feedback
                if (feedback.Waveform == KnownSimpleHapticsControllerWaveforms.BuzzContinuous)
                {
                    return feedback;
                }
            }

            // No supported feedback type has been found
            return null;
        }

        private async void StartVibrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vibrationDevice == null)
            {
                if (!_isCheckedForVibrationDevice)
                {
                    await GetVbrationDevice();
                }
            }

            if (_vibrationDevice != null && _buzzFeedback != null)
            {
                var hapticsController = _vibrationDevice.SimpleHapticsController;
                hapticsController.SendHapticFeedbackForDuration(_buzzFeedback, 1 /* full intensity */, TimeSpan.FromMilliseconds(int.Parse(TimeMillis.Text)));
            }
        }

        private async void CancelVibrationButton_Click(object sender, RoutedEventArgs e)
        {
            if (_vibrationDevice != null)
            {
                var hapticsController = _vibrationDevice.SimpleHapticsController;
                hapticsController.StopFeedback();
            }
            else
            {
                if (!_isCheckedForVibrationDevice)
                {
                    await GetVbrationDevice();
                }
            }
        }
    }
}
