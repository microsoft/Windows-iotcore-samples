using System;
using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Diagnostics;
using Windows.Devices.Gpio;
using Windows.UI.Core;
using System.Threading.Tasks;
using System.Runtime;
using Keg.DAL;
using Keg.DAL.Models;
using Windows.UI.Xaml.Input;
using System.Text;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Admin.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Admin_App2 : Page
    {
        public Admin_App2()
        {
            this.InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Admin_App3));
        }

        private void HyperlinkButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Admin_App6));
        }
    }
}
