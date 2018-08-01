using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;
using Keg.DAL.Models;
using Keg.DAL;
using System.Text;



// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace Admin.UWP
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class Admin_App3 : Page
    {
        public Admin_App3()
        {
            this.InitializeComponent();
            Window.Current.CoreWindow.KeyDown += OnKeyDown;
        }

        private StringBuilder sb = new StringBuilder();

        public void OnKeyDown(Windows.UI.Core.CoreWindow sender, Windows.UI.Core.KeyEventArgs e)
        {
            if (e.VirtualKey == Windows.System.VirtualKey.Enter)
            {
                Dictionary<string, string> user = new Dictionary<string, string>();
                user.Add($"User id: {sb.ToString()} ", $"Hash: {Hasher.GetSmartCardHash(sb.ToString())} ");
                if (sb.Length != 0)
                {
                    User u = new User();
                    {
                        u.HashCode = Hasher.GetSmartCardHash(sb.ToString());
                        u.IsApprover = false;
                    }
                    KegLogger.KegLogTrace($"Adding user User id: { sb.ToString()} Hash: { Hasher.GetSmartCardHash(sb.ToString())}", "App3:OnKeyDown", Microsoft.ApplicationInsights.DataContracts.SeverityLevel.Information, null);
                    User.AddUserAsync(u);
                    Window.Current.CoreWindow.KeyDown -= OnKeyDown;
                    this.Frame.Navigate(typeof(Admin_App4));
                }
                sb.Clear();
                return;
            }
            sb.Append((int)e.VirtualKey - 48);
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(Admin_App2));
        }
    }
}
