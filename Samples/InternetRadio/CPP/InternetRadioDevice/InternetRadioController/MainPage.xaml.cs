using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

using Windows.Devices.AllJoyn;
using com.microsoft.maker.InternetRadio;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace InternetRadioController
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public struct InternetRadioDeviceRegistrationInfo
        {
            public string Name { get; set; }
            public InternetRadioWatcher sender;
            public AllJoynServiceInfo args;
        }

        private ObservableCollection<InternetRadioDeviceRegistrationInfo> devices;

        private InternetRadioWatcher radioWatcher;

        private AllJoynBusAttachment alljoynBusAttachment;

        public MainPage()
        {
            this.InitializeComponent();

            devices = new ObservableCollection<InternetRadioDeviceRegistrationInfo>();
            this.AvailableRadios.ItemsSource = devices;

            alljoynBusAttachment = new AllJoynBusAttachment();
            
            radioWatcher = new InternetRadioWatcher(alljoynBusAttachment);
            
            radioWatcher.Added += RadioWatcher_Added;
            radioWatcher.Start();
        }

        private async void RadioWatcher_Added(InternetRadioWatcher sender, AllJoynServiceInfo args)
        {
            Debug.WriteLine(args.UniqueName);

            var about = await AllJoynAboutDataView.GetDataBySessionPortAsync(args.UniqueName, alljoynBusAttachment, args.SessionPort);

            if (null == about)
            {
                Debug.WriteLine("Unable to get AboutData for device: " + args.UniqueName);
                return;
            }

            await this.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, (() =>
             {
                 devices.Add(new InternetRadioDeviceRegistrationInfo()
                 {
                     Name = string.Format("{0} ({1})", about.AppName, about.DeviceName),
                     sender = sender,
                     args = args
                 });
             }));
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (null != this.AvailableRadios.SelectedItem)
            {
                this.Frame.Navigate(typeof(RadioControls), this.AvailableRadios.SelectedItem);
            }
        }
    }
}
